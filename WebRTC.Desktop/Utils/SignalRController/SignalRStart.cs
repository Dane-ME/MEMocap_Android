// System
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// External
using TCPping;
using Serilog;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
// Internal
using WebRTC.Desktop.Utils.Log;
using WebRTC.Desktop.Utils.RTCPeerConn;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using System.Net;
using SIPSorcery.SIP.App;
namespace WebRTC.Desktop.Utils.SignalRController
{
    public class SignalRStart : ISignalRStart, IDisposable
    {
        //HUB
        private string _ipAddress;
        private HubConnection? _connection;
        private HubConnectionState _currentstate;
        private string _deviceConnectionId = string.Empty;

        //RTC
        private IRTCPeerConnConfig _rtcPeerConnConfig;
        private RTCConfiguration _rTCConfig;
        private string _ffmpegLibFullPath;
        private readonly Dictionary<string, RTCPeerConnection> _peerConnectionsList
        = new Dictionary<string, RTCPeerConnection>();
        private readonly object _lockObject = new object();
        public HubConnectionState Currentstate
        {
            get => _currentstate;
            set
            {
                if(value != _currentstate)
                {
                    _currentstate = value;
                }
            }
        }

        //STREAM VIDEO
        private readonly Dictionary<string, VideoStreamManager> _videoStreams;

        //Event
        public event Action<RawImage, string>? OnVideoFrameReceived;
        public SignalRStart() 
        {
            // HUB SETTING
            IIPControl ipControl = new IPControl();
            _ipAddress = ipControl.GetIP() ?? "";
            if (string.IsNullOrEmpty(_ipAddress)) { 
                Serilog.Log.Error("Do not get IP this device"); 
                return; 
            }
            this._connection = new HubConnectionBuilder()
                .WithUrl($"http://{_ipAddress}:5000/videoHub", (option) =>
                {
                    option.Transports = HttpTransportType.WebSockets;
                })
                .Build();
            // RTC SETTING
            this._rtcPeerConnConfig = new RTCPeerConnConfig();
            _rTCConfig = this._rtcPeerConnConfig.GetRTCConfiguration();
            _ffmpegLibFullPath = this._rtcPeerConnConfig.GetffmpegLibFullPath();
            _peerConnectionsList = new Dictionary<string, RTCPeerConnection>();
            // STREAM VIDEO
            _videoStreams = new Dictionary<string, VideoStreamManager>();
        }
        // Public
        public async Task<bool> StartSignalRAsync()
        {
            if (this._connection == null) return false;
            try
            {
                await this._connection.StartAsync();
                Serilog.Log.Information($"[SignalR] Đã khởi động SignalR với địa chỉ {_ipAddress}");
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"[SignalR] Lỗi khi khởi động SignalR: {ex.Message}");
                return false;
            }
        }
        public HubConnectionState GetHubConnState()
        {
            UpdateHubConnState();
            return this.Currentstate;
        }
        public async Task StopSignalRAsync()
        {
            if (this._connection == null) return;
            try
            {
                await this._connection.StopAsync();
                Serilog.Log.Information($"[SignalR] Đã dừng SignalR với địa chỉ {_ipAddress}");
                DisposeSignalR();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"[SignalR] Lỗi khi dừng SignalR: {ex.Message}");
            }
        }
        public async Task<bool> RegisterAsCenterDevice()
        {
            if (this._connection == null) return false;
            try
            {
                await this._connection.InvokeAsync("RegisterAsCenterDevice");
                Serilog.Log.Information($"[SignalR] Đã đăng ký thành công");
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"[SignalR] Lỗi khi đăng ký: {ex.Message}");
                return false;
            }
        }
        public void CameraListUpdated(CancellationToken cancellationToken)
        {
            if (this._connection == null) return;
            this._connection.On("CameraListUpdated", (List<CameraInfor> cameras) =>
            {
                foreach (var camera in cameras)
                {
                    var streamManager = new VideoStreamManager(camera.ClientConnectionId ?? "unknown", _ffmpegLibFullPath);
                    var peerConnection = new RTCPeerConnection(_rTCConfig);


                    streamManager.VideoEndPoint.RestrictFormats(format => format.Codec == VideoCodecsEnum.VP8);
                    streamManager.VideoEndPoint.OnVideoSinkDecodedSampleFaster += (RawImage rawImage) =>
                    {
                        OnVideoFrameReceived?.Invoke(rawImage, camera.ClientConnectionId ?? "unknown");
                    };
                    MediaStreamTrack videoTrack = new MediaStreamTrack(streamManager.VideoEndPoint.GetVideoSinkFormats(), MediaStreamStatusEnum.RecvOnly);
                    peerConnection.addTrack(videoTrack);
                    peerConnection.OnVideoFrameReceived += streamManager.VideoEndPoint.GotVideoFrame;
                    peerConnection.OnVideoFormatsNegotiated += (formats) =>
                        streamManager.VideoEndPoint.SetVideoSinkFormat(formats.First());

                    #region CameraListUpdated.Logging
                    peerConnection.OnTimeout += (mediaType) => Serilog.Log.Debug($"Timeout on media {mediaType}.");

                    peerConnection.oniceconnectionstatechange += (state) =>
                    {
                        Serilog.Log.Debug($"ICE connection state changed to {state}.");
                    };

                    peerConnection.onconnectionstatechange += (state) =>
                    {
                        Serilog.Log.Debug($"Peer connection connected changed to {state}.");
                    };

                    peerConnection.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt) =>
                    {
                        Serilog.Log.Debug($"RTP {media} pkt received, SSRC {rtpPkt.Header.SyncSource}.");
                    };
                    #endregion

                    peerConnection.onicecandidate += (iceCandidate) =>
                    {
                        if (iceCandidate.candidate != null)
                        {
                            Serilog.Log.Debug($"ICE candidate: {iceCandidate}");
                            _ = this._connection.InvokeAsync("SendIceCandidate", camera.ClientConnectionId, iceCandidate, camera.ClientConnectionId);
                        }
                    };
                    AddPeerConnection(camera.ClientConnectionId ?? "unknown", peerConnection);
                    lock (_lockObject)
                    {
                        _videoStreams[camera.ClientConnectionId ?? "unknown"] = streamManager;
                    }
                    cancellationToken.Register(() => {
                        peerConnection.close();
                        _videoStreams.Remove(camera.ClientConnectionId ?? "unknown");
                    });
                }
            });
        }
        public void ClientDisconnected()
        {
            if (this._connection == null) return;
            this._connection.On("ClientDisconnected", async (string connectionId) =>
            {
                if (_peerConnectionsList.ContainsKey(connectionId))
                {
                    _peerConnectionsList[connectionId].close();
                    _peerConnectionsList.Remove(connectionId);
                    Serilog.Log.Debug($"Peer connection {connectionId} closed.");
                }
                _ = _connection.InvokeAsync("GetCenterDevices");
            });
        }
        public void ReceiveSdp()
        {
            if (this._connection == null) return;
            _ = this._connection.On("ReceiveSdp", async (string clientconnectionId, string sdp) =>
            {
                Serilog.Log.Debug($"Received SDP from {clientconnectionId} : {sdp}.");
                if (_peerConnectionsList.ContainsKey(clientconnectionId))
                {
                    try
                    {
                        RTCSessionDescriptionInit sdpInit = new();
                        RTCSessionDescriptionInit.TryParse(sdp, out sdpInit);
                        _peerConnectionsList[clientconnectionId].setRemoteDescription(sdpInit);
                        var answer = _peerConnectionsList[clientconnectionId].createAnswer();
                        await _peerConnectionsList[clientconnectionId].setLocalDescription(answer);
                        _ = _connection.InvokeAsync("SendSdp", clientconnectionId, TinyJson.JSONWriter.ToJson(answer), clientconnectionId);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error($"[SignalR] Lỗi khi nhận SDP: {ex.Message}");
                    }
                }
            });
        }
        public void ReceiveIceCandidate()
        {
            if (this._connection == null) return;
            _ = this._connection.On("ReceiveIceCandidate", async (string clientconnectionId, string candidate) =>
            {
                Serilog.Log.Debug($"Received ICE candidate from {clientconnectionId} : {candidate}.");
                if (_peerConnectionsList.ContainsKey(clientconnectionId))
                {
                    try
                    {
                        RTCIceCandidateInit candidateInit = new();
                        RTCIceCandidateInit.TryParse(candidate, out candidateInit);
                        _peerConnectionsList[clientconnectionId].addIceCandidate(candidateInit);
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error($"[SignalR] Lỗi khi nhận ICE candidate: {ex.Message}");
                    }
                }
            });
        }
        public void Registered()
        {
            if (this._connection == null) return;
            this._connection.On("Registered", (string connectionId, List<string> cameras) =>
            {
                _deviceConnectionId = connectionId;
                Serilog.Log.Debug($"Đã đăng ký thành công với ID: {connectionId}");
                if (cameras.Count == 0)
                {
                    Serilog.Log.Debug($"Không có camera nào được kết nối.");
                }
                else
                {
                    Serilog.Log.Debug($"Danh sách camera đã kết nối: {string.Join(", ", cameras)}");
                }
            });
        }
        public void Dispose()
        {
            foreach (var stream in _videoStreams.Values)
            {
                stream.VideoEndPoint.Dispose();
            }
            foreach (var connection in _peerConnectionsList.Values)
            {
                connection?.close();
            }
            _videoStreams.Clear();
            _peerConnectionsList.Clear();
            _connection?.DisposeAsync().GetAwaiter().GetResult();
        }
        // Function 
        private void DisposeSignalR()
        {
            if (this._connection == null) return;
            try
            {
                this._connection.DisposeAsync();
                Serilog.Log.Information($"[SignalR] Đã giải phóng SignalR với địa chỉ {_ipAddress}");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"[SignalR] Lỗi khi giải phóng SignalR: {ex.Message}");
            }
        }
        private void UpdateHubConnState()
        {
            if (this._connection == null) return;
            this.Currentstate = this._connection.State;
        }
        private void AddPeerConnection(string id, RTCPeerConnection connection)
        {
            lock (_lockObject)
            {
                _peerConnectionsList[id] = connection;
            }
        }

    }
}

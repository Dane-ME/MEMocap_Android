// LIBRARIES
using MEMocap.AndroidApp.Utils.RTCPeerConn;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;

using TCPpingMAUI;
// SYSTEM
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if ANDROID
using TCPpingMAUI.Platforms.Android;
#endif

namespace MEMocap.AndroidApp.Utils
{
    public interface IConnectionManager
    {
        
    }
    public class ConnectionManager : IConnectionManager
    {
        //HUB
        private string _ipAddress;
        private HubConnection? _connection;
        private HubConnectionState _currentstate;
        private string _centerDeviceConnectionId = string.Empty;
        private string _cameraDeviceConnectionIdInvoke = string.Empty;
        
        //RTC
        private IRTCPeerConnConfig _rtcPeerConnConfig;
        private RTCConfiguration _rTCConfig;
        public HubConnectionState Currentstate
        {
            get => _currentstate;
            set
            {
                if (value != _currentstate)
                {
                    _currentstate = value;
                }
            }
        }
        private SIPSorceryMedia.Encoders.Codecs.Vp8Codec _videoEncoder = new();
        private RTCPeerConnection _peerConnection;
        //STREAM VIDEO
        private const uint TIMESTAMP_FREQUENCY = 90000;
        // CameraService
#if ANDROID
        private CameraService _cameraService;
#elif WINDOWS
        //private CameraService _cameraService;
#elif IOS
        //private CameraService _cameraService;
#elif MACCATALYST
        //private CameraService _cameraService;
#endif

        //Event
        public ConnectionManager()
        {
            // HUB SETTING
            IIPControl ipControl = new IPControl();
            _ipAddress = ipControl.GetIP() ?? "";
            if (string.IsNullOrEmpty(_ipAddress)) return;
            this._connection = new HubConnectionBuilder()
                .WithUrl($"http://{_ipAddress}:5000/videoHub", (option) =>
                {
                    option.Transports = HttpTransportType.WebSockets;
                })
                .Build();
            // RTC SETTING
            this._rtcPeerConnConfig = new RTCPeerConnConfig();
            _rTCConfig = this._rtcPeerConnConfig.GetRTCConfiguration();
            _peerConnection = new RTCPeerConnection(_rTCConfig);
            var videoFormats = new List<VideoFormat> { (new VideoFormat() {Codec = VideoCodecsEnum.VP8 }) };
            MediaStreamTrack videoTrack = new MediaStreamTrack(videoFormats, MediaStreamStatusEnum.SendOnly);
            _peerConnection.addTrack(videoTrack);
            startCamera();
        }
        // PUBLIC
        public async Task<bool> StartSignalRAsync()
        {
            if (this._connection == null) return false;
            try
            {
                await this._connection.StartAsync();
                var cameraId = $"Camera-{DateTime.Now.AddMilliseconds}";
                await this._connection.InvokeAsync("RegisterAsCamera", cameraId);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public void ProcessFrame(byte[] yuvData)
        {
            var encodedSample = _videoEncoder.Encode(yuvData);
            uint durationRtpUnits = TIMESTAMP_FREQUENCY / 30; 
            _peerConnection.SendVideo(durationRtpUnits, encodedSample);
        }
        public void CenterDevicesUpdated()
        {

        }
        public void UpdateCodec()
        {

        }
        public void ReceiveSdp()
        {

        }
        public void ReceiveIceCandidate()
        {

        }

        public HubConnectionState GetHubConnState()
        {
            UpdateHubConnState();
            return this.Currentstate;
        }
        // PRIVATE
        private void UpdateHubConnState()
        {
            if (this._connection == null) return;
            this.Currentstate = this._connection.State;
        }
        private void startCamera() { 
        }
    }
}

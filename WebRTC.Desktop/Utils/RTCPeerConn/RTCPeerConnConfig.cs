// System
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
// External Library
using SIPSorcery.Net;
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using WebSocketSharp.Server;
using Org.BouncyCastle.Asn1.X509;
// Internal Library

namespace WebRTC.Desktop.Utils.RTCPeerConn
{
    class RTCPeerConnConfig : IRTCPeerConnConfig
    {
        private Dictionary<string, RTCPeerConnection> _peerConnectionsList { get; set; }
        private string STUN_URL = "stun:stun.l.google.com:19302";
        private RTCConfiguration _rTCConfig;
        private const string ffmpegLibFullPath = @"D:\WebRTC\WebRTC.Desktop\ffmpeg-7.1.1-full_build-shared\bin\";
        public RTCPeerConnConfig()
        {
            _peerConnectionsList = new Dictionary<string, RTCPeerConnection>();
            _rTCConfig = new RTCConfiguration()
            {
                iceServers = new List<RTCIceServer>() { new RTCIceServer() { urls = STUN_URL, } }
            };
        }
        public RTCConfiguration GetRTCConfiguration()
        {
            return _rTCConfig;
        }
        public string GetffmpegLibFullPath() => ffmpegLibFullPath;
    }
}

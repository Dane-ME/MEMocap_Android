using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRTC.Desktop.Utils
{
    public class VideoStreamManager
    {
        public string ConnectionId { get; }
        public FFmpegVideoEndPoint VideoEndPoint { get; }
        public Action<RawImage, string> OnVideoFrameReceived { get; set; }

        public VideoStreamManager(string connectionId, string ffmpegPath)
        {
            ConnectionId = connectionId;
            VideoEndPoint = new FFmpegVideoEndPoint();
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_VERBOSE, ffmpegPath);
        }
    }
}

using SIPSorcery.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEMocap.Android.Utils.RTCPeerConn
{
    public interface IRTCPeerConnConfig
    {
        /// <summary>
        /// Get the RTCConfiguration object.
        /// </summary>
        /// <returns></returns>
        RTCConfiguration GetRTCConfiguration();
        /// <summary>
        /// Get the full path of the ffmpeg library.
        /// </summary>
        /// <returns></returns>
        string GetffmpegLibFullPath();
    }
}

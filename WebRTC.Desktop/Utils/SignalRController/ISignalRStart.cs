using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRTC.Desktop.Utils.SignalRController
{
    public interface ISignalRStart
    {
        /// <summary>
        /// Start the SignalR connection.
        /// </summary>
        /// <returns></returns>
        Task<bool> StartSignalRAsync();
        /// <summary>
        /// Get the SignalR connection state.
        /// </summary>
        /// <returns></returns>
        HubConnectionState GetHubConnState();
        /// <summary>
        /// Register the camera device.
        /// </summary>
        /// <returns></returns>
        Task<bool> RegisterAsCenterDevice();
        /// <summary>
        /// update the camera list.
        /// </summary>
        /// <param name="cancellationToken"></param>
        void CameraListUpdated(CancellationToken cancellationToken);
        /// <summary>
        /// Client disconnected event.
        /// </summary>
        /// 
        void ClientDisconnected();
        /// <summary>
        /// Receive the SDP.
        /// </summary>
        void ReceiveSdp();
        /// <summary>
        /// Receive the ICE candidate.
        /// </summary>
        void ReceiveIceCandidate();
        /// <summary>
        /// Registered event.
        /// </summary>
        void Registered();
        /// <summary>
        /// dispose the SignalR connection.
        /// </summary>
        void Dispose();
    }
}

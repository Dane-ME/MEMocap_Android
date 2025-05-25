using System.Net.Sockets;
using System.Net;
using TCPping;
namespace WebRTC.Cores
{
    public class Ping
    {
        public Ping()
        {
            IIPControl ipControl = new IPControl();
            ITCPServerModule TCPServerModule = new TCPServerModule(5001);
            TCPServerModule.Listener();
        }
    }
}

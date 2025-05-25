using System.Net;
using System.Net.Sockets;
using System.Text;


namespace TCPping
{
    public interface ITCPClientModule
    {
        /// <summary>
        /// Ping all IPs in the same subnet as the local machine, except for 0, 127 and the local machine's IP.
        /// </summary>
        /// <param name="mess"></param>
        void Ping(string mess);
    }
    public class TCPClientModule : ITCPClientModule
    {
        private string? _baseip;
        private int _port = 5000;
        private string _message = "";
        private string _octet3 = "";
        public TCPClientModule(int port)
        {
            IIPControl ipControl = new IPControl();
            string[]? Octet = ipControl.GetOctet();
            if (Octet == null) return;
            _baseip = string.Join(".", Octet[0], Octet[1], Octet[2]);
            _port = port;
            _octet3 = Octet[3];
        }
        public void Ping(string mess)
        {
            _message = mess;
            int octet3; 
            int.TryParse(_octet3, out octet3);
            if (_baseip == null) return;
            for (int i = 1; i < 255; i++)
            {
                if(i == 0 || i == octet3 || i == 127) continue;
                string ipAddress = $"{_baseip}.{i}";
                IPAddress ip = IPAddress.Parse(ipAddress);
                byte[] pingMessage = Encoding.ASCII.GetBytes(_message);
                bool stop = false;
                PingMessage(ip, pingMessage, out stop);
                if (stop) break;
            }
        }
        private void PingMessage(IPAddress ip,  byte[] pingMessage, out bool stop)
        {
            bool flag = false;
            _ = Task.Run(async () =>
            {
                try
                {
                    using TcpClient client = new TcpClient();
                    await client.ConnectAsync(ip, this._port);
                    using NetworkStream stream = client.GetStream();
                    await stream.WriteAsync(pingMessage, 0, pingMessage.Length);
                    byte[] buffer = new byte[256];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"IP {ip} phản hồi: {response}");
                    if (response.Contains("true")) flag = true;
                }
                catch { }
            });
            stop = flag;
        }
    }
}

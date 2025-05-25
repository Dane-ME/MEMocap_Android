using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TCPpingMAUI
{
    public interface ITCPServerModule
    {
        /// <summary>
        /// Start the TCP server.
        /// </summary>
        Task Listener();
        /// <summary>
        /// Stop the TCP server.    
        /// </summary>
        Task Stop();
        /// <summary>
        /// Get the list of valid IP addresses that have been received.
        /// </summary>
        /// <returns></returns>
        List<string> GetIPValidList();
    }
    public class TCPServerModule : ITCPServerModule
    {
        private int _port = 5000;
        private bool _isRunning = false;
        private List<string> ipaddressList = new();
        private readonly TcpListener _listener;
        private readonly IIPControl _ipControl = new IPControl();
        private readonly CancellationTokenSource _cancellationTokenSource;
        public TCPServerModule(int port)
        {
            _port = port;
            _listener = new TcpListener(IPAddress.Any, _port);
            _cancellationTokenSource = new CancellationTokenSource();
        }
        public async Task Listener()
        {
            if (_isRunning) { Console.WriteLine("[TCP Server] Server đã chạy."); return; }
            try
            {
                _listener.Start();
                _isRunning = true;
                Console.WriteLine($"[TCP Server] Bắt đầu lắng nghe trên cổng {_port}...");
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                    _ = HandleClientAsync(client);
                }
            }
            finally
            {
                _isRunning = false;
                _listener.Stop();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            if (!client.Connected)
            {
                Console.WriteLine("[TCP Server] Client không kết nối.");
                client.Close();
                return;
            }
            try
            {
                using var stream = client.GetStream();
                byte[] buffer = new byte[32];
                int length = await stream.ReadAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);

                string message = Encoding.ASCII.GetString(buffer, 0, length);
                Console.WriteLine($"[TCP Server] Nhận từ {client.Client.RemoteEndPoint}: {message}");

                if (_ipControl.IsValidIPv4(message))
                {
                    ipaddressList.Add(message);
                    byte[] response = Encoding.ASCII.GetBytes(_ipControl.GetIP() ?? "");
                    await stream.WriteAsync(response, 0, response.Length, _cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TCP Server] Lỗi xử lý client: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }

        public Task Stop()
        {
            if (!_isRunning) {
                Console.WriteLine("[TCP Server] Server chưa chạy.");
                return Task.CompletedTask; 
            }
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _isRunning = false;
            Console.WriteLine("[TCP Server] Server đã dừng.");
            return Task.CompletedTask;
        }

        public List<string> GetIPValidList() => ipaddressList;
    }
}

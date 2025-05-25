using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace TCPping
{
    public interface IIPControl
    {
        /// <summary>
        /// Extract the local IP address from the machine.
        /// </summary>
        /// <returns></returns>
        string? GetIP();
        /// <summary>
        /// Get the octets of the local IP address.
        /// </summary>
        /// <returns></returns>
        string[]? GetOctet();
        /// <summary>
        /// Check if the given IP address is valid IPv4 address.
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        bool IsValidIPv4(string ip);
    }
    public class IPControl : IIPControl
    {
        private string? _ipaddress;
        private string? IPAddress
        {
            get { return _ipaddress; }
            set {
                _ipaddress = value;
                if (value != null)
                {
                    var octets = value.Split('.');
                    _octet = octets;
                }
            }
        }
        private string[] _octet { get; set; } = Array.Empty<string>();
        public void ExtractIPAddress()
        {
            this.IPAddress = GetDesktopIPAddress();
        }
        public static string GetDesktopIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return string.Empty;
        }
        public string? GetIP()
        {
            if (IPAddress == null)
                ExtractIPAddress();
            return IPAddress;
        }

        public string[]? GetOctet()
        {
            if(IPAddress == null)
                ExtractIPAddress();
            return _octet;
        }

        public bool IsValidIPv4(string ip)
        {
            if (System.Net.IPAddress.TryParse(ip, out var address))
            {
                return address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
            }
            return false;
        }
    }
}

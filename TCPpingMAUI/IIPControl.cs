namespace TCPpingMAUI
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
    public partial class IPControl : IIPControl
    {
        private string? _ipaddress;
        private string? LocalIPAddress
        {
            get { return _ipaddress; }
            set
            {
                _ipaddress = value;
                if (value != null)
                {
                    var octets = value.Split('.');
                    _octet = octets;
                }
            }
        }
        private string[] _octet { get; set; } = [];
        partial void GetMobileIPAddress();
        private void ExtractIPAddress()
        {
            GetMobileIPAddress();  
        }
        public string? GetIP()
        {
            if (LocalIPAddress == null)
                ExtractIPAddress();
            return LocalIPAddress;
        }

        public string[]? GetOctet()
        {
            if (LocalIPAddress == null)
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

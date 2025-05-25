using Android.Content;
using Android.Net.Wifi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TCPpingMAUI
{
    public partial class IPControl : IIPControl
    {
        partial void GetMobileIPAddress()
        {
            var context = Microsoft.Maui.ApplicationModel.Platform.AppContext;
            var wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);

            if (wifiManager == null || !wifiManager.IsWifiEnabled)
            {
                System.Diagnostics.Debug.WriteLine("Wi-Fi không được bật.");
                LocalIPAddress = string.Empty;
            }
            var wifiInfo = wifiManager.ConnectionInfo;
            if (wifiInfo == null || wifiInfo.IpAddress == 0)
            {
                System.Diagnostics.Debug.WriteLine("Không có kết nối Wi-Fi.");
                LocalIPAddress = string.Empty;
            }
            int ipAddress = wifiInfo.IpAddress;
            byte[] bytes = BitConverter.GetBytes(ipAddress);
            LocalIPAddress = new IPAddress(bytes).ToString(); 
        }
    }
}

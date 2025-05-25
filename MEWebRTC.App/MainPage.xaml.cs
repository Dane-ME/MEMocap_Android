using TCPpingMAUI;

namespace MEWebRTC.App
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            ITCPClientModule tcpClientModule = new TCPClientModule(5000);
            IIPControl ipControl = new IPControl();
            tcpClientModule.Ping(ipControl.GetIP() ?? "");
            
        }
    }

}

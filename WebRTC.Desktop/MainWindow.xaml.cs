using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using TCPping;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using SIPSorcery.Net;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography.Xml;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using WebRTC.Desktop.Utils.SignalRController;
namespace WebRTC.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private SignalRStart _signalRStart;
    public MainWindow()
    {
        InitializeComponent();
        _signalRStart = new SignalRStart();
        _ = _signalRStart.StartSignalRAsync();

    }
    private void Button_Click(object sender, RoutedEventArgs e)
    {
        CancellationTokenSource cts = new();
        _ = _signalRStart.RegisterAsCenterDevice();
        _signalRStart.CameraListUpdated(cts.Token);
        _signalRStart.ClientDisconnected();
        _signalRStart.ReceiveSdp();
        _signalRStart.ReceiveIceCandidate();
        _signalRStart.Registered();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        HubConnectionState a = _signalRStart.GetHubConnState();
    }

    private void Button_Click_2(object sender, RoutedEventArgs e)
    {
        _signalRStart.Dispose();
    }
}
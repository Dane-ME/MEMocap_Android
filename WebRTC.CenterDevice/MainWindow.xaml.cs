using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Web.WebView2.Wpf;
namespace WebRTC.CenterDevice;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var webView2 = new WebView2();
        webView2.Source = new Uri("D:\\WebRTC\\WebRTC.CenterDevice\\Documentation\\index.html"); // Replace with your URL
        MainContent.Child = webView2;
    }
}
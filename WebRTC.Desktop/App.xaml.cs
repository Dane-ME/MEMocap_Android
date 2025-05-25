// System
using System.Configuration;
using System.Data;
using System.Windows;
// External
// Internal
using WebRTC.Desktop.Utils.Log;
namespace WebRTC.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        LoggingSetup.SetupLogging();
    }
}


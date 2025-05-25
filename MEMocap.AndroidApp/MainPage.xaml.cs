using MEMocap.AndroidApp.Utils;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using TCPpingMAUI;

namespace MEMocap.AndroidApp
{
    public partial class MainPage : ContentPage
    {

        public MainPage()
        {
            InitializeComponent();
            _ = RequestCameraPermission();
            var cnm = new ConnectionManager();
        }
        public async Task RequestCameraPermission()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                // Permission granted, now you can initialize the camera
                // var cameraSetup = new CameraSetup(); // Hoặc inject thông qua DI
                // cameraSetup.InitializeCamera();
            }
            else
            {
                // Permission denied, handle accordingly
                await DisplayAlert("Permission Denied", "Camera permission is required to use this feature.", "OK");
            }
        }
    }

}

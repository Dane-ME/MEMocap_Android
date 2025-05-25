using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using System.Diagnostics;

namespace MEMocap.Android
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            OnAppearing();
            CameraPreiewControl.FrameArrived += CameraPreviewControl_FrameArrived;
        }

        private void CameraPreviewControl_FrameArrived(object sender, byte[] frameData)
        {
            // Ở đây, bạn nhận được byte[] của mỗi frame từ camera.
            // Đây là nơi bạn sẽ tích hợp với thư viện WebRTC của mình.
            // Dữ liệu frameData sẽ ở định dạng YUV_420_888 (hoặc đã chuyển đổi sang NV12/NV21 nếu bạn triển khai đúng).

            // Ví dụ: Ghi kích thước frame để kiểm tra
            Debug.WriteLine($"Received frame: {frameData.Length} bytes");

            // TODO: Gửi frameData này đến WebRTC PeerConnection
            // Thư viện WebRTC sẽ có các phương thức như AddVideoFrame, SendVideoData, v.v.
            // Tùy thuộc vào thư viện WebRTC bạn sử dụng (ví dụ: Google WebRTC Android native library).
        }
        private void OnStartWebRTCClicked(object sender, EventArgs e)
        {
            // Khởi tạo và thiết lập WebRTC PeerConnection ở đây.
            // Điều này bao gồm tạo offer/answer, ICE candidates, v.v.
            // WebRTC setup là một chủ đề phức tạp riêng.
            DisplayAlert("WebRTC", "Bắt đầu thiết lập WebRTC...", "OK");
        }
        public async Task RequestCameraPermissions()
        {
            PermissionStatus cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
            PermissionStatus audioStatus = await Permissions.CheckStatusAsync<Permissions.Microphone>(); // Nếu bạn cần âm thanh

            if (cameraStatus != PermissionStatus.Granted)
            {
                cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (audioStatus != PermissionStatus.Granted)
            {
                audioStatus = await Permissions.RequestAsync<Permissions.Microphone>();
            }

            if (cameraStatus != PermissionStatus.Granted || audioStatus != PermissionStatus.Granted)
            {
                await DisplayAlert("Quyền bị từ chối", "Ứng dụng không thể truy cập camera hoặc microphone. Vui lòng cấp quyền trong cài đặt ứng dụng.", "OK");
                // Có thể thoát ứng dụng hoặc vô hiệu hóa chức năng camera
            }
        }

        // Gọi phương thức này khi trang được tải hoặc trước khi cố gắng mở camera
        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RequestCameraPermissions();
        }
    }

}

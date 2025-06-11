using Android.Media;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEMocap.Android.Platforms.Android
{
    public partial class CameraPreviewHandler : ViewHandler<CameraPreview, CameraPreviewView>
    {
        public static PropertyMapper<CameraPreview, CameraPreviewHandler> PropertyMapper = new PropertyMapper<CameraPreview, CameraPreviewHandler>(ViewHandler.ViewMapper)
        {
            // Thêm các thuộc tính tùy chỉnh nếu bạn muốn điều khiển CameraPreview từ MAUI
            // Ví dụ: CameraPreview.IsFlashlightOnProperty
        };
        public CameraPreviewHandler() : base(PropertyMapper)
        {

        }
        protected override CameraPreviewView CreatePlatformView()
        {
            return new CameraPreviewView(MauiContext.Context);
        }
        protected override void ConnectHandler(CameraPreviewView platformView)
        {
            base.ConnectHandler(platformView);
            // Gắn sự kiện từ PlatformView vào Control
            platformView.OnFrameAvailable += PlatformView_OnFrameAvailable;
        }

        protected override void DisconnectHandler(CameraPreviewView platformView)
        {
            platformView.OnFrameAvailable -= PlatformView_OnFrameAvailable;
            base.DisconnectHandler(platformView);
        }
        private void PlatformView_OnFrameAvailable(object sender, byte[] frameData)
        {
            // Sự kiện này được kích hoạt khi một frame mới từ camera có sẵn.
            // Tại đây, bạn sẽ truyền frameData đến thư viện WebRTC của mình.
            // Control (CameraPreview của MAUI) có thể có một sự kiện hoặc phương thức để nhận dữ liệu này.
            VirtualView?.HandleFrame(frameData); // Ví dụ: Gọi một phương thức HandleFrame trên MAUI control.
        }

    }
}

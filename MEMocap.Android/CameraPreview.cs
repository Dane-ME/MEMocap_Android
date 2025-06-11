using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
#if ANDROID
using Android.Media;
#endif

namespace MEMocap.Android
{
    public class CameraPreview : View
    {
        public event EventHandler<byte[]> FrameArrived;

        public void HandleFrame(byte[] frameData)
        {
            // Phương thức này được gọi từ Platform Handler khi có frame mới
            FrameArrived?.Invoke(this, frameData);
        }
    }
}

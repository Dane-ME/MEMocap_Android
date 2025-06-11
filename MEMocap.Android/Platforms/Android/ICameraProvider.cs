using MEMocap.Android.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEMocap.Android.Platforms.Android
{
    public interface ICameraProvider
    {
        Task<CameraInfo> GetCameraAsync(CameraType cameraType = CameraType.Back);
        Task<List<CameraInfo>> GetAllCamerasAsync();
    }
}

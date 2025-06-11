using Android.Content;
using Android.Hardware.Camera2;
using MEMocap.Android.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using AndroidApplication = Android.App.Application;

namespace MEMocap.Android.Platforms.Android
{
    public class AndroidCameraProvider : ICameraProvider
    {
        private CameraManager _cameraManager;
        private readonly Dictionary<string, CameraInfo> _cameraCache = new();
        public AndroidCameraProvider()
        {
            var context = Platform.CurrentActivity ?? AndroidApplication.Context;
            _cameraManager = (CameraManager)context.GetSystemService(Context.CameraService);
        }
        public async Task<List<CameraInfo>> GetAllCamerasAsync()
        {
            var cameras = new List<CameraInfo>();

            try
            {
                var cameraIds = _cameraManager.GetCameraIdList();

                foreach (var id in cameraIds)
                {
                    if (_cameraCache.ContainsKey(id))
                    {
                        cameras.Add(_cameraCache[id]);
                        continue;
                    }

                    var characteristics = _cameraManager.GetCameraCharacteristics(id);
                    var cameraInfo = CreateCameraInfo(id, characteristics);
                    _cameraCache[id] = cameraInfo;
                    cameras.Add(cameraInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting camera list: {ex.Message}");
            }

            return cameras;
        }

        public async Task<CameraInfo> GetCameraAsync(CameraType cameraType = CameraType.Back)
        {
            var cameras = await GetAllCamerasAsync();
            var camera = cameras.FirstOrDefault(c => c.Type == cameraType);

            if (camera == null)
                throw new Exception($"Không tìm thấy camera loại: {cameraType}");

            return camera;
        }

        private CameraInfo CreateCameraInfo(string cameraId, CameraCharacteristics characteristics)
        {
            var facing = (int)characteristics.Get(CameraCharacteristics.LensFacing);
            var cameraType = GetCameraType(facing, characteristics);
            var displayName = GetDisplayName(cameraType, cameraId);

            return new CameraInfo
            {
                CameraId = cameraId,
                Type = cameraType,
                DisplayName = displayName,
                NativeCameraManager = _cameraManager,
                NativeCharacteristics = characteristics
            };
        }

        private CameraType GetCameraType(int facing, CameraCharacteristics characteristics)
        {
            // Xác định loại camera
            switch (facing)
            {
                case (int)LensFacing.Back:
                    return DetermineBackCameraType(characteristics);
                case (int)LensFacing.Front:
                    return CameraType.Front;
                case (int)LensFacing.External:
                    return CameraType.External;
                default:
                    return CameraType.Back;
            }
        }

        private CameraType DetermineBackCameraType(CameraCharacteristics characteristics)
        {
            // Phân biệt camera sau: thường, tele, ultra-wide
            var focalLengths = (float[])characteristics.Get(CameraCharacteristics.LensInfoAvailableFocalLengths);

            if (focalLengths != null && focalLengths.Length > 0)
            {
                var focalLength = focalLengths[0];

                // Ước tính dựa trên focal length (có thể cần điều chỉnh cho từng thiết bị)
                if (focalLength > 6.0f) return CameraType.Telephoto;
                if (focalLength < 3.0f) return CameraType.UltraWide;
            }

            return CameraType.Back;
        }

        private string GetDisplayName(CameraType type, string id)
        {
            return type switch
            {
                CameraType.Back => "Camera Sau",
                CameraType.Front => "Camera Trước",
                CameraType.Telephoto => "Camera Tele",
                CameraType.UltraWide => "Camera Ultra Wide",
                CameraType.External => "Camera Ngoài",
                _ => $"Camera {id}"
            };
        }
    }
}

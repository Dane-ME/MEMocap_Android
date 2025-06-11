using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// ANDROID
using Android.Content;
using Android.Hardware.Camera2;
using MEMocap.Android.Models;
using AndroidApp = Android.App.Application;
using CameraUtilSize = Android.Util.Size;
using AndroidUtilSizeF = Android.Util.SizeF;
using MicrosoftMauiGraphicSizeF = Microsoft.Maui.Graphics.SizeF;
using MEMocap.Android.Services;

namespace MEMocap.Android.Platforms.Android
{
    public class CameraService
    {
        private readonly ICameraProvider _cameraProvider;
        public CameraService(ICameraProvider cameraProvider = null)
        {
            _cameraProvider = cameraProvider ?? new AndroidCameraProvider();
        }
        public async Task<CameraIntrinsics> GetCameraIntrinsicsAsync(CameraType cameraType = CameraType.Back)
        {
            var camera = await _cameraProvider.GetCameraAsync(cameraType);
            return await GetIntrinsicsFromCamera(camera);
        }
        public async Task<List<CameraIntrinsics>> GetAllCameraIntrinsicsAsync()
        {
            var cameras = await _cameraProvider.GetAllCamerasAsync();
            var intrinsicsList = new List<CameraIntrinsics>();

            foreach (var camera in cameras)
            {
                try
                {
                    var intrinsics = await GetIntrinsicsFromCamera(camera);
                    intrinsics.CameraName = camera.DisplayName;
                    intrinsics.CameraId = camera.CameraId;
                    intrinsicsList.Add(intrinsics);
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng tiếp tục với camera khác
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi lấy thông số {camera.DisplayName}: {ex.Message}");
                }
            }

            return intrinsicsList;
        }

        private async Task<CameraIntrinsics> GetIntrinsicsFromCamera(CameraInfo cameraInfo)
        {
            var cameraChar = (CameraCharacteristics)cameraInfo.NativeCharacteristics;

            // Lấy thông số nội tại
            var intrinsicCalibration = (float[])cameraChar.Get(CameraCharacteristics.LensIntrinsicCalibration);
            var distortion = (float[])cameraChar.Get(CameraCharacteristics.LensDistortion);
            var sensorSize = (AndroidUtilSizeF)cameraChar.Get(CameraCharacteristics.SensorInfoPhysicalSize);
            var pixelArraySize = (CameraUtilSize)cameraChar.Get(CameraCharacteristics.SensorInfoPixelArraySize);

            // Tính toán thông số
            if (intrinsicCalibration?.Length >= 5)
            {
                return new CameraIntrinsics
                {
                    FocalLengthX = intrinsicCalibration[0],
                    FocalLengthY = intrinsicCalibration[1],
                    PrincipalPointX = intrinsicCalibration[2],
                    PrincipalPointY = intrinsicCalibration[3],
                    SkewFactor = intrinsicCalibration[4],

                    ImageWidth = pixelArraySize.Width,
                    ImageHeight = pixelArraySize.Height,

                    // Tính FOV từ focal length và sensor size
                    HorizontalFOV = (float)(2 * Math.Atan(sensorSize.Width / (2 * intrinsicCalibration[0])) * 180 / Math.PI),
                    VerticalFOV = (float)(2 * Math.Atan(sensorSize.Height / (2 * intrinsicCalibration[1])) * 180 / Math.PI),

                    // Distortion coefficients
                    RadialDistortion1 = distortion?.Length > 0 ? distortion[0] : 0,
                    RadialDistortion2 = distortion?.Length > 1 ? distortion[1] : 0,
                    TangentialDistortion1 = distortion?.Length > 2 ? distortion[2] : 0,
                    TangentialDistortion2 = distortion?.Length > 3 ? distortion[3] : 0
                };
            }
            else
            {
                // Fallback: ước tính từ FOV và kích thước sensor
                var fovRange = (float[])cameraChar.Get(CameraCharacteristics.LensInfoAvailableFocalLengths);
                var aperture = (float[])cameraChar.Get(CameraCharacteristics.LensInfoAvailableApertures);

                var sensorMauiSizeF = new MicrosoftMauiGraphicSizeF(sensorSize.Width, sensorSize.Height);

                return EstimateCameraIntrinsics(sensorMauiSizeF, pixelArraySize, fovRange?[0] ?? 4.0f);
            }
        }

        private CameraIntrinsics EstimateCameraIntrinsics(MicrosoftMauiGraphicSizeF sensorSize, CameraUtilSize pixelSize, float focalLengthMm)
        {
            float focalLengthPixelX = (focalLengthMm * pixelSize.Width) / sensorSize.Width;
            float focalLengthPixelY = (focalLengthMm * pixelSize.Height) / sensorSize.Height;

            return new CameraIntrinsics
            {
                FocalLengthX = focalLengthPixelX,
                FocalLengthY = focalLengthPixelY,
                PrincipalPointX = pixelSize.Width / 2.0f,
                PrincipalPointY = pixelSize.Height / 2.0f,
                ImageWidth = pixelSize.Width,
                ImageHeight = pixelSize.Height,
                HorizontalFOV = (float)(2 * Math.Atan(sensorSize.Width / (2 * focalLengthMm)) * 180 / Math.PI),
                VerticalFOV = (float)(2 * Math.Atan(sensorSize.Height / (2 * focalLengthMm)) * 180 / Math.PI)
            };
        }
    }
}

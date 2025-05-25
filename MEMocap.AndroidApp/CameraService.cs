using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MEMocap.Android
{
    public class CameraService
    {
        private ProcessCameraProvider _cameraProvider;
        private ImageAnalysis _imageAnalysis;
        public event Action<byte[]> OnFrameCaptured;

        public async Task PlatformStartCameraCapture()
        {
            var context = Platform.AppContext;
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(context);
            _cameraProvider = await Task.Run(() =>
            {
                var tcs = new TaskCompletionSource<ProcessCameraProvider>();
                cameraProviderFuture.AddListener(
                    new Runnable(() =>
                    {
                        try
                        {
                            var provider = (ProcessCameraProvider)cameraProviderFuture.Get();
                            tcs.SetResult(provider);
                        }
                        catch (System.Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    }),
                    ContextCompat.GetMainExecutor(context)
                );
                return tcs.Task;
            });
            _imageAnalysis = new ImageAnalysis.Builder()
                .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                .Build();

            _imageAnalysis.SetAnalyzer(ContextCompat.GetMainExecutor(context), new FrameAnalyzer(OnFrameReceived));
            var cameraSelector = new CameraSelector.Builder()
                .RequireLensFacing(CameraSelector.LensFacingBack)
                .Build();

            _cameraProvider.UnbindAll();
            _cameraProvider.BindToLifecycle((ILifecycleOwner)Platform.CurrentActivity, cameraSelector, _imageAnalysis);
        }
        private byte[] GetYuvDataFromImage(IImageProxy image)
        {
            var planes = image.GetPlanes();
            int width = image.Width;
            int height = image.Height;
            byte[] yuvData = new byte[width * height * 3 / 2]; // YUV_420_888 format

            // Y plane (luminance)
            var yPlane = planes[0];
            var yBuffer = yPlane.Buffer;
            var yRowStride = yPlane.RowStride;
            int yOffset = 0;
            for (int y = 0; y < height; y++)
            {
                yBuffer.Position(y * yRowStride);
                yBuffer.Get(yuvData, yOffset, width);
                yOffset += width;
            }

            // U plane (chrominance, subsampled)
            var uPlane = planes[1];
            var uBuffer = uPlane.Buffer;
            var uRowStride = uPlane.RowStride;
            var uPixelStride = uPlane.PixelStride;
            int uOffset = width * height;
            for (int y = 0; y < height / 2; y++)
            {
                uBuffer.Position(y * uRowStride);
                for (int x = 0; x < width / 2; x++)
                {
                    yuvData[uOffset++] = (byte)(uBuffer.Get(y * uRowStride + x * uPixelStride) & 0xFF);
                }
            }

            // V plane (chrominance, subsampled)
            var vPlane = planes[2];
            var vBuffer = vPlane.Buffer;
            var vRowStride = vPlane.RowStride;
            var vPixelStride = vPlane.PixelStride;
            for (int y = 0; y < height / 2; y++)
            {
                vBuffer.Position(y * vRowStride);
                for (int x = 0; x < width / 2; x++)
                {
                    yuvData[uOffset++] = (byte)(vBuffer.Get(y * vRowStride + x * vPixelStride) & 0xFF);
                }
            }

            return yuvData;
        }
        private void OnFrameReceived(IImageProxy image)
        {
            try
            {
                var yuvData = GetYuvDataFromImage(image);
                OnFrameCaptured?.Invoke(yuvData);
            }
            finally
            {
                image.Close(); // Close IImageProxy to avoid memory leaks
            }
        }
        public async Task PlatformStopCameraCapture()
        {
            _cameraProvider?.UnbindAll();
            _imageAnalysis?.ClearAnalyzer();
        }
        private class FrameAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
        {
            private readonly Action<IImageProxy> _onFrameReceived;

            public FrameAnalyzer(Action<IImageProxy> onFrameReceived)
            {
                _onFrameReceived = onFrameReceived;
            }

            public void Analyze(IImageProxy image)
            {
                _onFrameReceived(image);
            }
        }
    }
}

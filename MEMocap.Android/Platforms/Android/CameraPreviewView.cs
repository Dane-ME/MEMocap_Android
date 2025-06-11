//System
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Android
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;
using Android.OS;
using Android.Util;
using AndroidUtilSize = Android.Util.Size;
using AndroidMediaImage = Android.Media.Image;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Android.Nfc;
using Java.Util;
using Android.Hardware.Lights;
using MEMocap.Android.Models;
using MEMocap.Android.Utils;
using static Android.Views.Choreographer;
namespace MEMocap.Android.Platforms.Android
{
    public class CameraPreviewView : FrameLayout, TextureView.ISurfaceTextureListener, IDisposable
    {
        private TextureView? _textureView;
        private CameraManager? _cameraManager;
        private string? _cameraId;
        private AndroidUtilSize? _previewSize;
        private CameraDevice? _cameraDevice;
        private CameraCaptureSession? _captureSession;
        private CaptureRequest.Builder? _previewRequestBuilder;
        private ImageReader? _imageReader;
        private HandlerThread? _backgroundThread;
        private readonly ICameraProvider _cameraProvider;
        private Handler? _backgroundHandler;
        private CameraInfo? _currentCamera;
        private ImageAvailableListener? _imageAvailableListener;
        private bool _disposed = false;

        public event EventHandler<byte[]>? OnFrameAvailable;

        public CameraPreviewView(Context context) : base(context)
        {
            _cameraProvider = new AndroidCameraProvider();
            InitializeCameraPreview();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                StopBackgroundThread();
                CloseCamera();
                _textureView?.Dispose();
                _disposed = true;
            }
        }
        private void InitializeCameraPreview()
        {
            _textureView = new TextureView(Context);
            _textureView.SurfaceTextureListener = this;
            AddView(_textureView);
        }
        public async void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            StartBackgroundThread();
            await OpenCameraAsync(width, height);
        }
        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            StopBackgroundThread();
            CloseCamera();
            return true;
        }
        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            throw new NotImplementedException();
        }
        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {
            //
        }
        private void StartBackgroundThread()
        {
            _backgroundThread = new HandlerThread("CameraBackground");
            _backgroundThread.Start();
            _backgroundHandler = new Handler(_backgroundThread.Looper);
        }
        private void StopBackgroundThread()
        {
            if (_backgroundThread != null)
            {
                _backgroundThread.QuitSafely();
                try
                {
                    _backgroundThread.Join();
                    _backgroundThread = null;
                    _backgroundHandler = null;
                }
                catch (InterruptedException e)
                {
                    Log.Error("CameraPreviewView", "Error stopping background thread: " + e.Message);
                }
            }
        }
        private async Task OpenCameraAsync(int w, int h)
        {
            try
            {
                _currentCamera = await _cameraProvider.GetCameraAsync(CameraType.Back);
                _cameraManager = (CameraManager)_currentCamera.NativeCameraManager;
                _cameraId = _currentCamera.CameraId;

                CameraCharacteristics characteristics = _currentCamera.NativeCharacteristics as CameraCharacteristics;
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                if (map == null) return;
                _previewSize = ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))), w, h);
                _imageReader = ImageReader.NewInstance(_previewSize.Width, _previewSize.Height, ImageFormatType.Yuv420888, 6);
                _imageAvailableListener = new ImageAvailableListener(this);
                _imageReader.SetOnImageAvailableListener(_imageAvailableListener, _backgroundHandler);

                //Java.Lang.Object intrins = characteristics.Get(CameraCharacteristics.LensIntrinsicCalibration);

                _cameraManager.OpenCamera(_cameraId, new CameraStateCallback(this), _backgroundHandler);

            }
            catch (System.Exception e)
            {
                Log.Error("CameraPreviewView", "Error opening camera: " + e.Message);
            }
        }
        private void CreateCameraPreviewSession()
        {
            try
            {
                SurfaceTexture texture = _textureView.SurfaceTexture;
                if (texture == null)
                {
                    Log.Error("CameraPreviewView", "Texture is null, cannot create preview session.");
                    return;
                }

                if (_imageAvailableListener == null)
                {
                    Log.Error("CameraPreviewView", "ImageAvailableListener is null, cannot create preview session.");
                    return;
                }

                texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);

                Surface previewSurface = new Surface(texture);
                // ✅ SỬA: Sử dụng encoder surface thay vì ImageReader surface
                Surface encoderSurface = _imageAvailableListener.GetEncoderSurface();
                Surface imageReaderSurface = _imageReader.Surface; // Giữ ImageReader để trigger OnImageAvailable

                if (encoderSurface == null)
                {
                    Log.Error("CameraPreviewView", "Encoder surface is null, cannot create preview session.");
                    return;
                }

                _previewRequestBuilder = _cameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                _previewRequestBuilder.AddTarget(previewSurface);
                _previewRequestBuilder.AddTarget(encoderSurface); // ✅ Camera ghi trực tiếp vào encoder
                _previewRequestBuilder.AddTarget(imageReaderSurface); // Trigger cho OnImageAvailable

                List<Surface> surfaces = new List<Surface> { previewSurface, encoderSurface, imageReaderSurface };

                _cameraDevice.CreateCaptureSession(surfaces, new CameraCaptureSessionCallback(this), _backgroundHandler);
            }
            catch (CameraAccessException e)
            {
                Log.Error("CameraPreviewView", "CameraAccessException creating preview session: " + e.Message);
            }
        }
        private class CameraStateCallback : CameraDevice.StateCallback
        {
            private CameraPreviewView _parent;

            public CameraStateCallback(CameraPreviewView parent)
            {
                _parent = parent;
            }

            public override void OnOpened(CameraDevice cameraDevice)
            {
                _parent._cameraDevice = cameraDevice;
                _parent.CreateCameraPreviewSession();
            }

            public override void OnDisconnected(CameraDevice cameraDevice)
            {
                cameraDevice.Close();
                _parent._cameraDevice = null;
            }

            public override void OnError(CameraDevice cameraDevice, [GeneratedEnum] CameraError error)
            {
                Log.Error("CameraPreviewView", $"Camera error: {error}");
                cameraDevice.Close();
                _parent._cameraDevice = null;
                _parent.StopBackgroundThread();
            }
        }
        private class CameraCaptureSessionCallback : CameraCaptureSession.StateCallback
        {
            private CameraPreviewView _parent;

            public CameraCaptureSessionCallback(CameraPreviewView parent)
            {
                _parent = parent;
            }

            public override void OnConfigured(CameraCaptureSession session)
            {
                if (_parent._cameraDevice == null)
                {
                    return;
                }

                _parent._captureSession = session;
                try
                {
                    _parent._previewRequestBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.ContinuousPicture);
                    _parent._previewRequestBuilder.Set(CaptureRequest.ControlAeMode, (int)ControlAEMode.OnAutoFlash);

                    _parent._captureSession.SetRepeatingRequest(_parent._previewRequestBuilder.Build(), null, _parent._backgroundHandler);
                }
                catch (CameraAccessException e)
                {
                    Log.Error("CameraPreviewView", "Failed to start camera preview: " + e.Message);
                }
            }

            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                Log.Error("CameraPreviewView", "Failed to configure camera preview session.");
            }
        }
        private class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
        {
            private readonly CameraPreviewView _parent;
            private readonly VP8SurfaceEncoder _encoder;
            private readonly Queue<byte[]> _frameBufferPool;
            private const int MAX_FRAME_POOL_SIZE = 3;

            public ImageAvailableListener(CameraPreviewView parent)
            {
                _parent = parent;
                _encoder = new VP8SurfaceEncoder(_parent._previewSize.Width, _parent._previewSize.Height);
                _frameBufferPool = new Queue<byte[]>();
            }
            public Surface GetEncoderSurface()
            {
                return _encoder?.GetInputSurface();
            }
            public void OnImageAvailable(ImageReader? reader)
            {
                AndroidMediaImage? image = reader?.AcquireLatestImage();
                try
                {
                    // Drain encoder để lấy encoded frames
                    byte[]? encodedFrame = _encoder.DrainEncoder();
                    if (encodedFrame != null)
                    {
                        // ✅ Sử dụng pooled buffer để copy frame data
                        byte[] frameData = GetFrameBuffer(encodedFrame.Length);
                        Array.Copy(encodedFrame, 0, frameData, 0, encodedFrame.Length);

                        // Invoke với copy, giảm pressure trên original buffer
                        _parent.OnFrameAvailable?.Invoke(_parent, frameData);

                        // Note: frameData sẽ được GC handle, hoặc có thể implement return pool
                    }

                }
                catch (System.Exception e)
                {
                    Log.Error("CameraPreviewView", "Error acquiring image: " + e.Message);
                }
                finally
                {
                    image?.Close(); // ✅ Quan trọng: Release ImageReader buffer
                }
            }

            private byte[] GetFrameBuffer(int size)
            {
                // Simple pooling implementation
                if (_frameBufferPool.Count > 0)
                {
                    var buffer = _frameBufferPool.Dequeue();
                    if (buffer.Length >= size)
                    {
                        return buffer;
                    }
                    // Buffer quá nhỏ, return về pool và tạo mới
                    if (_frameBufferPool.Count < MAX_FRAME_POOL_SIZE)
                    {
                        _frameBufferPool.Enqueue(buffer);
                    }
                }

                return new byte[size];
            }

            public new void Dispose()
            {
                _encoder?.Dispose();
                _frameBufferPool.Clear();
            }
        }
        private void CloseCamera()
        {
            if (_captureSession != null)
            {
                _captureSession.Close();
                _captureSession = null;
            }
            if (_cameraDevice != null)
            {
                _cameraDevice.Close();
                _cameraDevice = null;
            }
            if (_imageReader != null)
            {
                _imageReader.Close();
                _imageReader = null;
            }
            if (_imageAvailableListener != null)
            {
                _imageAvailableListener.Dispose();
                _imageAvailableListener = null;
            }
        }
        private AndroidUtilSize ChooseOptimalSize(AndroidUtilSize[] choices, int textureWidth, int textureHeight)
        {
            // Collect the supported resolutions that are at least as large as the preview surface
            var bigEnough = new List<AndroidUtilSize>();
            // Collect the supported resolutions that are smaller than the preview surface
            var notBigEnough = new List<AndroidUtilSize>();
            int w = textureWidth;
            int h = textureHeight;

            foreach (var option in choices)
            {
                if (option.Width == option.Height * h / w &&
                    option.Width >= w && option.Height >= h)
                {
                    bigEnough.Add(option);
                }
                else
                {
                    notBigEnough.Add(option);
                }
            }

            // Pick the smallest of those big enough. If there is no such size, pick the largest of those not big enough.
            if (bigEnough.Count > 0)
            {
                return bigEnough.OrderBy(s => s.Width * s.Height).First();
            }
            else if (notBigEnough.Count > 0)
            {
                return notBigEnough.OrderByDescending(s => s.Width * s.Height).First();
            }
            else
            {
                Log.Error("CameraPreviewView", "Couldn't find any suitable preview size");
                return choices[0];
            }
        }
    }
}

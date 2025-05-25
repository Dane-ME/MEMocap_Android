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
using Size = Android.Util.Size;
using Image = Android.Media.Image;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Android.Nfc;
using Java.Util;
using Android.Hardware.Lights;
namespace MEMocap.Android.Platforms.Android
{
    public class CameraPreviewView : FrameLayout, TextureView.ISurfaceTextureListener
    {
        private TextureView _textureView;
        private CameraManager _cameraManager;
        private string _cameraId;
        private Size _previewSize;
        private CameraDevice _cameraDevice;
        private CameraCaptureSession _captureSession;
        private CaptureRequest.Builder _previewRequestBuilder;
        private ImageReader _imageReader;
        private HandlerThread _backgroundThread;
        private Handler _backgroundHandler;

        public event EventHandler<byte[]> OnFrameAvailable;
        public CameraPreviewView(Context context) : base(context)
        {
            InitializeCameraPreview();
        }
        private void InitializeCameraPreview()
        {
            _textureView = new TextureView(Context);
            _textureView.SurfaceTextureListener = this;
            AddView(_textureView);

            _cameraManager = (CameraManager)Context.GetSystemService(Context.CameraService);
            
        }
        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
            StartBackgroundThread();
            OpenCamera(width, height);
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
        private void OpenCamera(int w, int h)
        {
            try
            {
                string[] cameraIds = _cameraManager.GetCameraIdList();
                if(cameraIds.Length == 0)
                {
                    Log.Error("CameraPreviewView", "No cameras found.");
                    return;
                }
                _cameraId = cameraIds[0];
                CameraCharacteristics characteristics = _cameraManager.GetCameraCharacteristics(_cameraId);
                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);
                if (map == null) return;
                _previewSize = ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(SurfaceTexture))), w, h);
                _imageReader = ImageReader.NewInstance(_previewSize.Width, _previewSize.Height, ImageFormatType.Yuv420888, 2);
                _imageReader.SetOnImageAvailableListener(new ImageAvailableListener(this), _backgroundHandler);

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
                texture.SetDefaultBufferSize(_previewSize.Width, _previewSize.Height);

                Surface previewSurface = new Surface(texture);
                Surface readerSurface = _imageReader.Surface; // Surface từ ImageReader

                _previewRequestBuilder = _cameraDevice.CreateCaptureRequest(CameraTemplate.Preview);
                //_previewRequestBuilder.AddTarget(previewSurface);
                _previewRequestBuilder.AddTarget(readerSurface); // Thêm ImageReader vào pipeline

                //List<Surface> surfaces = new List<Surface> { previewSurface, readerSurface };
                //List<Surface> surfaces = new List<Surface> { previewSurface };
                List<Surface> surfaces = new List<Surface> { readerSurface };

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
            private CameraPreviewView _parent;

            public ImageAvailableListener(CameraPreviewView parent)
            {
                _parent = parent;
            }

            public void OnImageAvailable(ImageReader? reader)
            {
                Image image = null;
                try
                {
                    image = reader.AcquireLatestImage();
                    if (image == null) return;

                    // Lấy dữ liệu ảnh
                    // Ở đây, image.Format là YUV_420_888
                    // Bạn cần chuyển đổi nó sang định dạng phù hợp cho WebRTC (ví dụ: NV12, NV21 hoặc RGB)
                    // và sau đó bắn sự kiện OnFrameAvailable.
                    // Việc chuyển đổi YUV -> NV12/NV21/RGB là một phần phức tạp và yêu cầu xử lý byte buffer.
                    // Ví dụ đơn giản (chưa tối ưu và có thể cần điều chỉnh định dạng):

                    byte[] imageData = ConvertYUV420888ToNv12(image); // Hoặc định dạng khác
                    _parent.OnFrameAvailable?.Invoke(_parent, imageData);

                    // Để chuyển đổi từ YUV_420_888 sang byte[] cho mục đích hiển thị/ghi:
                    // ByteBuffer buffer = image.GetPlanes()[0].Buffer;
                    // byte[] bytes = new byte[buffer.Remaining()];
                    // buffer.Get(bytes);
                    // _parent.OnFrameAvailable?.Invoke(_parent, bytes); // Gửi frame raw Y

                }
                catch (System.Exception e)
                {
                    Log.Error("CameraPreviewView", "Error acquiring image: " + e.Message);
                }
                finally
                {
                    image?.Close();
                }
            }
            private byte[] ConvertYUV420888ToNv12(Image image)
            {
                var yPlane = image.GetPlanes()[0];
                var uPlane = image.GetPlanes()[1];
                var vPlane = image.GetPlanes()[2];

                var yBuffer = yPlane.Buffer;
                var uBuffer = uPlane.Buffer;
                var vBuffer = vPlane.Buffer;

                int width = image.Width;
                int height = image.Height;

                int ySize = width * height;
                int uvSize = width * height / 4; // Kích thước của U và V plane (đã subsample)

                byte[] nv12 = new byte[ySize + uvSize * 2]; // NV12: Y + чередование UV

                // Copy Y plane
                yBuffer.Get(nv12, 0, ySize);

                // Interleave U and V planes for NV12
                int uvPos = ySize;
                for (int i = 0; i < uvSize; i++)
                {
                    nv12[uvPos++] = (byte)(uBuffer.Get() & 0xFF); // U
                    nv12[uvPos++] = (byte)(vBuffer.Get() & 0xFF); // V
                }

                return nv12;
            }
            private byte[] ConvertYuv420888ToNv21(Image image)
            {
                // Đây là một ví dụ đơn giản và có thể cần tối ưu hóa hiệu suất
                // và xử lý đầy đủ các planes (Y, U, V) cùng với stride.
                // Đối với WebRTC, thường cần định dạng NV12 hoặc I420.
                // Một cách chính xác hơn là tham khảo các thư viện chuyển đổi hoặc mã nguồn WebRTC.

                var yPlane = image.GetPlanes()[0];
                var uPlane = image.GetPlanes()[1];
                var vPlane = image.GetPlanes()[2];

                var yBuffer = yPlane.Buffer;
                var uBuffer = uPlane.Buffer;
                var vBuffer = vPlane.Buffer;

                int ySize = yBuffer.Remaining();
                int uSize = uBuffer.Remaining();
                int vSize = vBuffer.Remaining();

                // Kích thước của ảnh NV21: Y + UV (UV có kích thước bằng Y/2)
                byte[] nv21 = new byte[ySize + uSize + vSize];

                yBuffer.Get(nv21, 0, ySize);

                // Interleave U and V planes for NV21
                int uvStartIndex = ySize;
                byte[] uData = new byte[uSize];
                byte[] vData = new byte[vSize];
                uBuffer.Get(uData);
                vBuffer.Get(vData);

                // NV21: YYYYYYYY VVUU
                // NV12: YYYYYYYY UUVV
                // Nếu bạn cần NV21 (YV12 với UV hoán đổi)
                // YUV_420_888 planes are Y, U, V
                // NV21 needs Y, V, U interleaved
                // NV12 needs Y, U, V interleaved

                // Việc chuyển đổi chính xác từ YUV_420_888 (3 planes, có thể có padding) sang NV12/NV21
                // (2 planes, không có padding hoặc padding theo một quy tắc khác) là phức tạp.
                // Các thư viện WebRTC thường có các helper để xử lý định dạng này.
                // Tham khảo mã nguồn AOSP hoặc các ví dụ của thư viện WebRTC.
                // Ví dụ đơn giản này có thể không đúng cho mọi trường hợp.

                // Để đơn giản (và có thể không hoàn hảo cho WebRTC nếu không có chuyển đổi chính xác):
                // Chỉ copy Y, sau đó là U, sau đó là V (đây không phải NV21 chuẩn)
                // Array.Copy(uData, 0, nv21, uvStartIndex, uSize);
                // Array.Copy(vData, 0, nv21, uvStartIndex + uSize, vSize);

                // Để chuyển đổi YUV_420_888 (Android) sang NV21 hoặc I420 (chuẩn WebRTC),
                // bạn cần xử lý từng pixel hoặc block pixel, có tính đến rowStride và pixelStride.
                // Điều này đòi hỏi kiến thức sâu về xử lý hình ảnh.

                // Một cách tiếp cận đơn giản hơn cho WebRTC là sử dụng một thư viện
                // hoặc component Android đã tích hợp sẵn khả năng cấp feed video.

                return nv21; // Trả về mảng byte chưa hoàn chỉnh, cần triển khai đúng.
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
        }
        private Size ChooseOptimalSize(Size[] choices, int textureWidth, int textureHeight)
        {
            // Collect the supported resolutions that are at least as large as the preview surface
            var bigEnough = new List<Size>();
            // Collect the supported resolutions that are smaller than the preview surface
            var notBigEnough = new List<Size>();
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

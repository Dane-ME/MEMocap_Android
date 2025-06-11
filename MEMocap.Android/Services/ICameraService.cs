using MEMocap.Android.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MEMocap.Android.Services
{
    /// <summary>
    /// Abstraction for camera operations to improve testability and separation of concerns
    /// </summary>
    public interface ICameraService : IDisposable
    {
        event EventHandler<CameraFrameEventArgs> FrameAvailable;
        event EventHandler<CameraErrorEventArgs> CameraError;
        event EventHandler<CameraStateEventArgs> CameraStateChanged;

        Task<bool> InitializeAsync(CameraType cameraType = CameraType.Back, CancellationToken cancellationToken = default);
        Task<bool> StartPreviewAsync(CancellationToken cancellationToken = default);
        Task StopPreviewAsync(CancellationToken cancellationToken = default);
        Task<CameraIntrinsics?> GetCameraIntrinsicsAsync(CameraType cameraType = CameraType.Back);
        
        bool IsInitialized { get; }
        bool IsPreviewActive { get; }
        CameraInfo? CurrentCamera { get; }
    }

    /// <summary>
    /// Event arguments for camera frame data
    /// </summary>
    public class CameraFrameEventArgs : EventArgs
    {
        public byte[] FrameData { get; }
        public int Width { get; }
        public int Height { get; }
        public long Timestamp { get; }
        public ImageFormat Format { get; }

        public CameraFrameEventArgs(byte[] frameData, int width, int height, long timestamp, ImageFormat format)
        {
            FrameData = frameData ?? throw new ArgumentNullException(nameof(frameData));
            Width = width;
            Height = height;
            Timestamp = timestamp;
            Format = format;
        }
    }

    /// <summary>
    /// Event arguments for camera errors
    /// </summary>
    public class CameraErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string ErrorMessage { get; }
        public CameraErrorType ErrorType { get; }

        public CameraErrorEventArgs(Exception exception, string errorMessage, CameraErrorType errorType)
        {
            Exception = exception;
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            ErrorType = errorType;
        }
    }

    /// <summary>
    /// Event arguments for camera state changes
    /// </summary>
    public class CameraStateEventArgs : EventArgs
    {
        public CameraState PreviousState { get; }
        public CameraState CurrentState { get; }

        public CameraStateEventArgs(CameraState previousState, CameraState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }

    /// <summary>
    /// Camera state enumeration
    /// </summary>
    public enum CameraState
    {
        Uninitialized,
        Initializing,
        Initialized,
        Starting,
        Active,
        Stopping,
        Stopped,
        Error
    }

    /// <summary>
    /// Camera error types for better error categorization
    /// </summary>
    public enum CameraErrorType
    {
        Initialization,
        Configuration,
        Permission,
        Hardware,
        Network,
        Unknown
    }

    /// <summary>
    /// Image format enumeration
    /// </summary>
    public enum ImageFormat
    {
        YUV420_888,
        NV12,
        NV21,
        RGB,
        RGBA
    }
}

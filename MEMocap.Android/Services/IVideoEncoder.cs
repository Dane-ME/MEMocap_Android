using System;
using System.Threading;
using System.Threading.Tasks;

namespace MEMocap.Android.Services
{
    /// <summary>
    /// Abstraction for video encoding operations
    /// </summary>
    public interface IVideoEncoder : IDisposable
    {
        event EventHandler<EncodedFrameEventArgs> FrameEncoded;
        event EventHandler<EncoderErrorEventArgs> EncodingError;

        Task<bool> InitializeAsync(VideoEncoderConfig config, CancellationToken cancellationToken = default);
        Task<bool> EncodeFrameAsync(byte[] frameData, long timestamp, CancellationToken cancellationToken = default);
        Task FlushAsync(CancellationToken cancellationToken = default);
        
        bool IsInitialized { get; }
        VideoEncoderConfig? Configuration { get; }
    }

    /// <summary>
    /// Video encoder configuration
    /// </summary>
    public class VideoEncoderConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitRate { get; set; } = 2_000_000; // 2 Mbps default
        public int FrameRate { get; set; } = 30;
        public int KeyFrameInterval { get; set; } = 1; // seconds
        public VideoCodec Codec { get; set; } = VideoCodec.VP8;
        public ColorFormat InputColorFormat { get; set; } = ColorFormat.YUV420_888;
        public EncoderProfile Profile { get; set; } = EncoderProfile.Baseline;
        public bool LowLatencyMode { get; set; } = true;

        public void Validate()
        {
            if (Width <= 0) throw new ArgumentException("Width must be positive", nameof(Width));
            if (Height <= 0) throw new ArgumentException("Height must be positive", nameof(Height));
            if (BitRate <= 0) throw new ArgumentException("BitRate must be positive", nameof(BitRate));
            if (FrameRate <= 0) throw new ArgumentException("FrameRate must be positive", nameof(FrameRate));
            if (KeyFrameInterval <= 0) throw new ArgumentException("KeyFrameInterval must be positive", nameof(KeyFrameInterval));
        }
    }

    /// <summary>
    /// Event arguments for encoded frame data
    /// </summary>
    public class EncodedFrameEventArgs : EventArgs
    {
        public byte[] EncodedData { get; }
        public long Timestamp { get; }
        public bool IsKeyFrame { get; }
        public int Size { get; }

        public EncodedFrameEventArgs(byte[] encodedData, long timestamp, bool isKeyFrame)
        {
            EncodedData = encodedData ?? throw new ArgumentNullException(nameof(encodedData));
            Timestamp = timestamp;
            IsKeyFrame = isKeyFrame;
            Size = encodedData.Length;
        }
    }

    /// <summary>
    /// Event arguments for encoder errors
    /// </summary>
    public class EncoderErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string ErrorMessage { get; }
        public EncoderErrorType ErrorType { get; }

        public EncoderErrorEventArgs(Exception exception, string errorMessage, EncoderErrorType errorType)
        {
            Exception = exception;
            ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
            ErrorType = errorType;
        }
    }

    /// <summary>
    /// Video codec enumeration
    /// </summary>
    public enum VideoCodec
    {
        VP8,
        VP9,
        H264,
        H265,
        AV1
    }

    /// <summary>
    /// Color format enumeration
    /// </summary>
    public enum ColorFormat
    {
        YUV420_888,
        YUV420_Planar,
        YUV420_SemiPlanar,
        NV12,
        NV21
    }

    /// <summary>
    /// Encoder profile enumeration
    /// </summary>
    public enum EncoderProfile
    {
        Baseline,
        Main,
        High,
        ConstrainedBaseline,
        ConstrainedHigh
    }

    /// <summary>
    /// Encoder error types
    /// </summary>
    public enum EncoderErrorType
    {
        Initialization,
        Configuration,
        Encoding,
        Hardware,
        Memory,
        Unknown
    }
}

using System;

namespace MEMocap.Android.Configuration
{
    /// <summary>
    /// Centralized configuration for camera operations
    /// </summary>
    public class CameraConfiguration
    {
        // Camera Settings
        public int DefaultWidth { get; set; } = 1920;
        public int DefaultHeight { get; set; } = 1080;
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan InitializationTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        // Image Processing Settings
        public int ImageReaderBufferCount { get; set; } = 2;
        public bool UseOptimizedImageProcessing { get; set; } = true;
        public int MaxFrameBufferSize { get; set; } = 1024 * 1024 * 10; // 10MB

        // Background Thread Settings
        public string BackgroundThreadName { get; set; } = "CameraBackground";
        public int BackgroundThreadTimeout { get; set; } = 10000; // 10 seconds

        // Encoder Settings
        public VideoEncoderConfiguration VideoEncoder { get; set; } = new();

        // Logging Settings
        public bool EnableDebugLogging { get; set; } = false;
        public bool EnablePerformanceLogging { get; set; } = true;

        public void Validate()
        {
            if (DefaultWidth <= 0)
                throw new ArgumentException("DefaultWidth must be positive", nameof(DefaultWidth));
            
            if (DefaultHeight <= 0)
                throw new ArgumentException("DefaultHeight must be positive", nameof(DefaultHeight));
            
            if (MaxRetryAttempts < 0)
                throw new ArgumentException("MaxRetryAttempts cannot be negative", nameof(MaxRetryAttempts));
            
            if (InitializationTimeout <= TimeSpan.Zero)
                throw new ArgumentException("InitializationTimeout must be positive", nameof(InitializationTimeout));
            
            if (OperationTimeout <= TimeSpan.Zero)
                throw new ArgumentException("OperationTimeout must be positive", nameof(OperationTimeout));

            VideoEncoder.Validate();
        }
    }

    /// <summary>
    /// Video encoder specific configuration
    /// </summary>
    public class VideoEncoderConfiguration
    {
        public string CodecName { get; set; } = "video/x-vnd.on2.vp8";
        public int BitRate { get; set; } = 2_000_000; // 2 Mbps
        public int FrameRate { get; set; } = 30;
        public int KeyFrameInterval { get; set; } = 1; // seconds
        public int Priority { get; set; } = 0; // Highest priority
        public int Latency { get; set; } = 1; // Low latency
        public int RepeatPreviousFrameAfter { get; set; } = 1_000_000; // 1 second in microseconds
        public int TimeoutMicroseconds { get; set; } = 10_000; // 10ms
        public int MaxEncodedFrameBufferSize { get; set; } = 100; // Max frames in buffer

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(CodecName))
                throw new ArgumentException("CodecName cannot be null or empty", nameof(CodecName));
            
            if (BitRate <= 0)
                throw new ArgumentException("BitRate must be positive", nameof(BitRate));
            
            if (FrameRate <= 0)
                throw new ArgumentException("FrameRate must be positive", nameof(FrameRate));
            
            if (KeyFrameInterval <= 0)
                throw new ArgumentException("KeyFrameInterval must be positive", nameof(KeyFrameInterval));
        }
    }

    /// <summary>
    /// Network configuration for streaming
    /// </summary>
    public class NetworkConfiguration
    {
        public string DefaultServerUrl { get; set; } = "http://localhost:5000";
        public string HubEndpoint { get; set; } = "/videoHub";
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan ReconnectionDelay { get; set; } = TimeSpan.FromSeconds(5);
        public int MaxReconnectionAttempts { get; set; } = 5;
        public bool EnableAutomaticReconnection { get; set; } = true;

        // WebRTC Configuration
        public string[] IceServers { get; set; } = new[]
        {
            "stun:stun.l.google.com:19302",
            "stun:stun1.l.google.com:19302"
        };

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(DefaultServerUrl))
                throw new ArgumentException("DefaultServerUrl cannot be null or empty", nameof(DefaultServerUrl));
            
            if (string.IsNullOrWhiteSpace(HubEndpoint))
                throw new ArgumentException("HubEndpoint cannot be null or empty", nameof(HubEndpoint));
            
            if (ConnectionTimeout <= TimeSpan.Zero)
                throw new ArgumentException("ConnectionTimeout must be positive", nameof(ConnectionTimeout));
            
            if (ReconnectionDelay <= TimeSpan.Zero)
                throw new ArgumentException("ReconnectionDelay must be positive", nameof(ReconnectionDelay));
            
            if (MaxReconnectionAttempts < 0)
                throw new ArgumentException("MaxReconnectionAttempts cannot be negative", nameof(MaxReconnectionAttempts));
        }
    }
}

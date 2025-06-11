using Android.Media;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using JavaLangSystem = Java.Lang.JavaSystem;

namespace MEMocap.Android.Platforms.Android
{
    public class VP8SurfaceEncoder : IDisposable
    {
        private readonly MediaCodec? _codec;
        private readonly Surface? _inputSurface;
        private readonly Queue<byte[]> _encodedFrames;
        private readonly Queue<byte[]> _bufferPool;
        private bool _disposed = false;
        private const int TIMEOUT_US = 10000;
        private const int MAX_POOL_SIZE = 5;
        private const int INITIAL_BUFFER_SIZE = 150000; // 150KB để handle I-frames

        public VP8SurfaceEncoder(int width, int height)
        {
            _encodedFrames = new Queue<byte[]>();
            _bufferPool = new Queue<byte[]>();

            // Pre-allocate some buffers
            for (int i = 0; i < 3; i++)
            {
                _bufferPool.Enqueue(new byte[INITIAL_BUFFER_SIZE]);
            }

            try
            {
                _codec = MediaCodec.CreateEncoderByType("video/x-vnd.on2.vp8");
                var format = MediaFormat.CreateVideoFormat("video/x-vnd.on2.vp8", width, height);

                format.SetInteger(MediaFormat.KeyBitRate, 2_000_000);
                format.SetInteger(MediaFormat.KeyFrameRate, 30);
                format.SetInteger(MediaFormat.KeyIFrameInterval, 3);
                format.SetInteger(MediaFormat.KeyColorFormat, 0x7f000789);
                format.SetInteger(MediaFormat.KeyPriority, 0);
                format.SetInteger(MediaFormat.KeyLatency, 1);

                _codec.Configure(format, null, null, MediaCodecConfigFlags.Encode);
                _inputSurface = _codec.CreateInputSurface();
                _codec.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing encoder: {ex.Message}");
                throw;
            }
        }

        public byte[]? DrainEncoder()
        {
            var bufferInfo = new MediaCodec.BufferInfo();
            int outputBufferIndex = _codec.DequeueOutputBuffer(bufferInfo, TIMEOUT_US);

            if (outputBufferIndex >= 0)
            {
                var outputBuffer = _codec.GetOutputBuffer(outputBufferIndex);
                if (outputBuffer != null && bufferInfo.Size > 0)
                {
                    // ✅ Sử dụng pooled buffer thay vì tạo mới
                    byte[] encodedData = GetPooledBuffer(bufferInfo.Size);

                    // Copy data từ MediaCodec buffer
                    outputBuffer.Position(bufferInfo.Offset);
                    outputBuffer.Get(encodedData, 0, bufferInfo.Size);

                    // Tạo array với đúng size để return (tránh expose buffer lớn hơn cần thiết)
                    byte[] result = new byte[bufferInfo.Size];
                    Array.Copy(encodedData, 0, result, 0, bufferInfo.Size);

                    // Return buffer về pool ngay lập tức
                    ReturnBufferToPool(encodedData);

                    _encodedFrames.Enqueue(result);
                }

                _codec.ReleaseOutputBuffer(outputBufferIndex, false);
                return _encodedFrames.Count > 0 ? _encodedFrames.Dequeue() : null;
            }

            return null;
        }

        public Surface? GetInputSurface() => _inputSurface;

        private byte[] GetPooledBuffer(int requiredSize)
        {
            // Try to get a buffer from pool that's large enough
            if (_bufferPool.Count > 0)
            {
                var buffer = _bufferPool.Dequeue();
                if (buffer.Length >= requiredSize)
                {
                    return buffer;
                }
                // Buffer too small, return to pool and create new one
                ReturnBufferToPool(buffer);
            }

            // Create new buffer with some extra space for future use
            return new byte[Math.Max(requiredSize, INITIAL_BUFFER_SIZE)];
        }

        private void ReturnBufferToPool(byte[] buffer)
        {
            if (_bufferPool.Count < MAX_POOL_SIZE && buffer.Length >= INITIAL_BUFFER_SIZE / 2)
            {
                _bufferPool.Enqueue(buffer);
            }
            // If pool is full or buffer too small, let GC handle it
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
                _inputSurface?.Release();
                _codec?.Stop();
                _codec?.Release();

                // Clear buffer pools
                _encodedFrames.Clear();
                _bufferPool.Clear();

                _disposed = true;
            }
        }
    }
    public class VP8Encoder : IDisposable
    {
        private readonly MediaCodec? _codec;
        private readonly int _width;
        private readonly int _height;
        private const int TIMEOUT_US = 10000; // 10ms timeout
        private readonly Queue<byte[]> _encodedFrames;
        private bool _disposed = false;

        public VP8Encoder(int width, int height)
        {
            _width = width;
            _height = height;
            _encodedFrames = new Queue<byte[]>();

            try
            {
                _codec = MediaCodec.CreateByCodecName("video/x-vnd.on2.vp8");
                var format = MediaFormat.CreateVideoFormat("video/x-vnd.on2.vp8", _width, _height);

                // Configure encoding parameters
                format.SetInteger(MediaFormat.KeyBitRate, 2_000_000);        // 2 Mbps
                format.SetInteger(MediaFormat.KeyFrameRate, 30);             // 30fps
                format.SetInteger(MediaFormat.KeyIFrameInterval, 1);         // Key frame every second
                format.SetInteger(MediaFormat.KeyColorFormat,
                    (int)MediaCodecCapabilities.Formatyuv420flexible);  // YUV420 flexible
                format.SetInteger(MediaFormat.KeyPriority, 0);              // Highest priority
                format.SetInteger(MediaFormat.KeyLatency, 1);               // Low latency mode
                format.SetInteger(MediaFormat.KeyRepeatPreviousFrameAfter, 1000000); // 1 sec

                _codec?.Configure(format, null, null, MediaCodecConfigFlags.Encode);
                _codec?.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing MediaCodec: {ex.Message}");
                _codec?.Release();
                throw;
            }
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
                try
                {
                    _codec?.Flush();  // Flush pending data
                    _codec?.Stop();
                    _codec?.Release();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing VP8Encoder: {ex.Message}");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
        public void Encode(byte[] inputFrame)
        {
            try
            {
                int inputBufferIndex = _codec.DequeueInputBuffer(TIMEOUT_US); // 10ms timeout
                if (inputBufferIndex >= 0)
                {
                    var inputBuffer = _codec.GetInputBuffer(inputBufferIndex);
                    inputBuffer?.Clear(); // Clear the buffer before putting data
                    inputBuffer?.Put(inputFrame);

                    _codec.QueueInputBuffer(
                        inputBufferIndex,
                        0,
                        inputFrame.Length,
                        JavaLangSystem.NanoTime() / 1000,
                        MediaCodecBufferFlags.None);
                }

                var bufferInfo = new MediaCodec.BufferInfo();
                int outputBufferIndex = _codec.DequeueOutputBuffer(bufferInfo, TIMEOUT_US);

                while (outputBufferIndex >= 0)
                {
                    var outputBuffer = _codec.GetOutputBuffer(outputBufferIndex);
                    if (outputBuffer != null && bufferInfo.Size > 0)
                    {
                        byte[] encodedData = new byte[bufferInfo.Size];
                        outputBuffer.Position(bufferInfo.Offset);
                        outputBuffer.Get(encodedData, 0, bufferInfo.Size);
                        _encodedFrames.Enqueue(encodedData);
                    }

                    _codec.ReleaseOutputBuffer(outputBufferIndex, false);
                    outputBufferIndex = _codec.DequeueOutputBuffer(bufferInfo, TIMEOUT_US);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during encoding: {ex.Message}");
                throw;
            }
        }
        public byte[] GetEncodedFrame()
        {
            return _encodedFrames.Count > 0 ? _encodedFrames.Dequeue() : null;
        }
    }
}

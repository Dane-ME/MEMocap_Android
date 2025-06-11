using System;
using System.Runtime.CompilerServices;

#if ANDROID
using Android.Util;
#endif


namespace MEMocap.Android.Utils
{
    /// <summary>
    /// Centralized logging utility for camera operations with structured logging
    /// </summary>
    public static class CameraLogger
    {
        private const string TAG = "MEMocap.Camera";

        public static void LogInfo(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var className = GetClassName(filePath);
#if ANDROID
            Log.Info(TAG, $"[{className}.{memberName}] {message}");
#endif

        }

        public static void LogWarning(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var className = GetClassName(filePath);
#if ANDROID
            Log.Warn(TAG, $"[{className}.{memberName}] {message}");
#endif
        }

        public static void LogError(string message, Exception? exception = null, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var className = GetClassName(filePath);
            var logMessage = $"[{className}.{memberName}] {message}";
            
            if (exception != null)
            {
                logMessage += $"\nException: {exception.GetType().Name}: {exception.Message}";
                if (exception.InnerException != null)
                {
                    logMessage += $"\nInner Exception: {exception.InnerException.GetType().Name}: {exception.InnerException.Message}";
                }
                logMessage += $"\nStack Trace: {exception.StackTrace}";
            }
#if ANDROID
            Log.Error(TAG, logMessage);
#endif
        }

        public static void LogDebug(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string filePath = "")
        {
            var className = GetClassName(filePath);
#if ANDROID
            Log.Debug(TAG, $"[{className}.{memberName}] {message}");
#endif
        }

        private static string GetClassName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "Unknown";

            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return fileName;
        }
    }

    /// <summary>
    /// Camera-specific exception types for better error handling
    /// </summary>
    public class CameraInitializationException : Exception
    {
        public CameraInitializationException(string message) : base(message) { }
        public CameraInitializationException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class CameraConfigurationException : Exception
    {
        public CameraConfigurationException(string message) : base(message) { }
        public CameraConfigurationException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class CameraOperationException : Exception
    {
        public CameraOperationException(string message) : base(message) { }
        public CameraOperationException(string message, Exception innerException) : base(message, innerException) { }
    }
}

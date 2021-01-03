using System;
using System.Runtime.CompilerServices;

namespace GameLoop.Utilities.Logs
{
    public static partial class Logger
    {
        private static Action<string> _onInfoCallback;
        private static Action<string> _onWarningCallback;
        private static Action<string> _onErrorCallback;

        private static object _sync;

        public static void Initialize(Action<string> onInfo, Action<string> onWarning, Action<string> onError)
        {
            _onInfoCallback    = onInfo;
            _onWarningCallback = onWarning;
            _onErrorCallback   = onError;

            _sync = new object();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Log(string message, Action<string> callback)
        {
            if (message  == null) return;
            if (callback == null) return;
            
            lock (_sync) callback.Invoke(message);
        }

        public static void Info(string message)
        {
            Log(message, _onInfoCallback);
        }

        public static void Info(string message, params object[] parameters)
        {
            Log(string.Format(message, parameters), _onInfoCallback);
        }
        
        public static void Warning(string message)
        {
            Log(message, _onWarningCallback);
        }

        public static void Warning(string message, params object[] parameters)
        {
            Log(string.Format(message, parameters), _onWarningCallback);
        }

        public static void Warning(Exception exception)
        {
            Log($"{exception.Message}\n{exception.StackTrace}", _onWarningCallback);
        }
        
        public static void Error(string message)
        {
            Log(message, _onErrorCallback);
        }

        public static void Error(string message, params object[] parameters)
        {
            Log(string.Format(message, parameters), _onErrorCallback);
        }

        public static void Error(Exception exception)
        {
            Log($"{exception.Message}\n{exception.StackTrace}", _onErrorCallback);
        }
    }
}
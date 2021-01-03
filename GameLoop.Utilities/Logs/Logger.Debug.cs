using System;
using System.Diagnostics;

namespace GameLoop.Utilities.Logs
{
    public static partial class Logger
    {
        [Conditional("DEBUG")]
        public static void DebugInfo(string message)
        {
            Info(message);
        }

        [Conditional("DEBUG")]
        public static void DebugInfo(string message, params object[] parameters)
        {
            Info(message, parameters);
        }
        
        [Conditional("DEBUG")]
        public static void DebugWarning(string message)
        {
            Warning(message);
        }

        [Conditional("DEBUG")]
        public static void DebugWarning(string message, params object[] parameters)
        {
            Warning(message, parameters);
        }

        [Conditional("DEBUG")]
        public static void DebugWarning(Exception exception)
        {
            Warning(exception);
        }
        
        [Conditional("DEBUG")]
        public static void DebugError(string message)
        {
            Error(message);
        }

        [Conditional("DEBUG")]
        public static void DebugError(string message, params object[] parameters)
        {
            Error(message, parameters);
        }

        [Conditional("DEBUG")]
        public static void DebugError(Exception exception)
        {
            Error(exception);
        }
    }
}
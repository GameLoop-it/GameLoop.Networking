/*
The MIT License (MIT)

Copyright (c) 2020 Emanuele Manzione

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GameLoop.Utilities.Logs
{
    public static partial class Logger
    {
        [Conditional("DEBUG")]
        public static void Debug(string                    message,
                                 [CallerFilePath]   string callerPath   = "",
                                 [CallerLineNumber] long   callerLine   = 0,
                                 [CallerMemberName] string callerMember = "")
        {
            var context = _contextPool.GetDebugContext();
            context.Message      = message;
            context.CallerFile   = callerPath;
            context.CallerMethod = callerMember;
            context.CallerLine   = callerLine;

            Log(context, _onDebugCallback);

            _contextPool.ReturnDebugContext(context);
        }

        [Conditional("DEBUG")]
        public static void DebugInfo(string                    message,
                                     [CallerFilePath]   string callerPath   = "",
                                     [CallerLineNumber] long   callerLine   = 0,
                                     [CallerMemberName] string callerMember = "")
        {
            var context = _contextPool.GetInfoContext();
            context.Message      = message;
            context.CallerFile   = callerPath;
            context.CallerMethod = callerMember;
            context.CallerLine   = callerLine;

            Log(context, _onInfoCallback);

            _contextPool.ReturnInfoContext(context);
        }

        [Conditional("DEBUG")]
        public static void DebugWarning(string                    message,
                                        [CallerFilePath]   string callerPath   = "",
                                        [CallerLineNumber] long   callerLine   = 0,
                                        [CallerMemberName] string callerMember = "")
        {
            var context = _contextPool.GetWarningContext();
            context.Message      = message;
            context.CallerFile   = callerPath;
            context.CallerMethod = callerMember;
            context.CallerLine   = callerLine;

            Log(context, _onWarningCallback);

            _contextPool.ReturnWarningContext(context);
        }

        [Conditional("DEBUG")]
        public static void DebugWarning(Exception                 exception,
                                        [CallerFilePath]   string callerPath   = "",
                                        [CallerLineNumber] long   callerLine   = 0,
                                        [CallerMemberName] string callerMember = "")
        {
            var context = _contextPool.GetWarningContext();
            context.Message      = $"{exception.Message}\n{exception.StackTrace}";
            context.CallerFile   = callerPath;
            context.CallerMethod = callerMember;
            context.CallerLine   = callerLine;

            Log(context, _onWarningCallback);

            _contextPool.ReturnWarningContext(context);
        }

        [Conditional("DEBUG")]
        public static void DebugError(string                    message,
                                      [CallerFilePath]   string callerPath   = "",
                                      [CallerLineNumber] long   callerLine   = 0,
                                      [CallerMemberName] string callerMember = "")
        {
            var context = _contextPool.GetErrorContext();
            context.Message      = message;
            context.CallerFile   = callerPath;
            context.CallerMethod = callerMember;
            context.CallerLine   = callerLine;

            Log(context, _onErrorCallback);

            _contextPool.ReturnErrorContext(context);
        }

        [Conditional("DEBUG")]
        public static void DebugError(Exception                 exception,
                                      [CallerFilePath]   string callerPath   = "",
                                      [CallerLineNumber] long   callerLine   = 0,
                                      [CallerMemberName] string callerMember = "")
        {
            var context = _contextPool.GetErrorContext();
            context.Message      = $"{exception.Message}\n{exception.StackTrace}";
            context.CallerFile   = callerPath;
            context.CallerMethod = callerMember;
            context.CallerLine   = callerLine;

            Log(context, _onErrorCallback);

            _contextPool.ReturnErrorContext(context);
        }
    }
}
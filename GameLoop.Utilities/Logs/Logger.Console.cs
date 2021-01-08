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

namespace GameLoop.Utilities.Logs
{
    public static partial class Logger
    {
        public static void InitializeForConsole()
        {
            Initialize(
                OnDebugCallbackForConsole,
                OnInfoCallbackForConsole,
                OnWarningCallbackForConsole,
                OnErrorCallbackForConsole
            );
        }
        
        private static void OnDebugCallbackForConsole(LogContext context)
        {
            var previousConsoleColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(context.Level);
            Console.Write($"[{context.CallerFile}::{context.CallerMethod}@{context.CallerLine}] ");
            Console.WriteLine(context.Message);

            Console.ForegroundColor = previousConsoleColor;
        }

        private static void OnInfoCallbackForConsole(LogContext context)
        {
            var previousConsoleColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(context.Level);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{context.CallerFile}::{context.CallerMethod}@{context.CallerLine}] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(context.Message);

            Console.ForegroundColor = previousConsoleColor;
        }

        private static void OnWarningCallbackForConsole(LogContext context)
        {
            var previousConsoleColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(context.Level);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{context.CallerFile}::{context.CallerMethod}@{context.CallerLine}] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(context.Message);

            Console.ForegroundColor = previousConsoleColor;
        }

        private static void OnErrorCallbackForConsole(LogContext context)
        {
            var previousConsoleColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(context.Level);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[{context.CallerFile}::{context.CallerMethod}@{context.CallerLine}] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(context.Message);

            Console.ForegroundColor = previousConsoleColor;
        }
    }
}
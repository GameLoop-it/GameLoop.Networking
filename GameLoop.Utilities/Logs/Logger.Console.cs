using System;

namespace GameLoop.Utilities.Logs
{
    public static partial class Logger
    {
        public static void InitializeForConsole()
        {
            Initialize(
                OnInfoCallbackForConsole,
                OnWarningCallbackForConsole,
                OnErrorCallbackForConsole
            );
        }

        private static void OnInfoCallbackForConsole(string message)
        {
            var previousConsoleColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[INF] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);

            Console.ForegroundColor = previousConsoleColor;
        }

        private static void OnWarningCallbackForConsole(string message)
        {
            var previousConsoleColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[WRN] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);

            Console.ForegroundColor = previousConsoleColor;
        }

        private static void OnErrorCallbackForConsole(string message)
        {
            var previousConsoleColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("[ERR] ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine(message);

            Console.ForegroundColor = previousConsoleColor;
        }
    }
}
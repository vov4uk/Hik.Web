using HikConsole.SDK;
using System;

namespace HikConsole.Helpers
{
    public static class ConsoleHelper
    {
        public static void WriteLine(string str = null, ConsoleColor foreground = ConsoleColor.Gray, DateTime? timeStamp = null)
        {

            Console.ForegroundColor = foreground;
            if (timeStamp == null)
            {
                Console.WriteLine(str);
            }
            else
            {
                Console.WriteLine($"{timeStamp} : {str}");
            }
            Console.ResetColor();
        }

        public static void Write(string str = null, ConsoleColor foreground = ConsoleColor.Gray, DateTime? timeStamp = null)
        {
            Console.ForegroundColor = foreground;
            if (timeStamp == null)
            {
                Console.Write(str);
            }
            else
            {
                Console.Write($"{timeStamp} : {str}");
            }
            Console.ResetColor();
        }

        public static void ColorWriteLine(string str = null, ConsoleColor foreground = ConsoleColor.Gray, DateTime? timeStamp = null)
        {
            Write(timeStamp: timeStamp);
            WriteLine(str, foreground);
        }

        public static void PrintError(string method, string msg = "")
        {
            ColorWriteLine($"{method} failed, error code = {SDKWrapper.GetLastError()} : {msg}", ConsoleColor.Red, DateTime.Now);
        }
    }
}

using System;

namespace HikConsole
{
    public static class ConsoleHelper
    {
        public static void WriteLine(string str = null, ConsoleColor foreground = ConsoleColor.Gray, DateTime? timeStamp = null)
        {

            Console.ForegroundColor = foreground;
            if(timeStamp == null)
            {
                Console.WriteLine(str);
            }
            else
            {
                Console.WriteLine($"{timeStamp} : {str}");
            }            
            Console.ResetColor();
        }

        public static void Write(string str = null, ConsoleColor foreground = ConsoleColor.Gray, DateTime ? timeStamp = null)
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

        public static void PrintError(string method, string msg = "")
        {
            Write(timeStamp: DateTime.Now);
            WriteLine($"{method} failed, error code = {SDK.NET_DVR_GetLastError()} : {msg}", ConsoleColor.Red);
        }
    }
}

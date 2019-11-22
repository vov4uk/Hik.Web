using System;
using System.Diagnostics.CodeAnalysis;

namespace HikConsole.Helpers
{
    [ExcludeFromCodeCoverage]
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

        public static void PrintLine(int length = 100)
        {
            WriteLine(new string('_', length));
        }
    }
}

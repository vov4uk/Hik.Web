using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Autofac;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;

namespace HikConsole
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static void Main()
        {
            var container = AppBootstrapper.ConfigureIoc();
            var downloader = container.Resolve<HikDownloader>();
            downloader.ExecuteAsync("configuration.json").GetAwaiter().GetResult();

            WaitForExit();
        }

        private static void WaitForExit()
        {
            Console.WriteLine("Press \'q\' to quit");
            while (Console.ReadKey() != new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false))
            {
                // do nothing
            }
        }
    }
}

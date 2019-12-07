using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Autofac;
using HikConsole.Abstraction;
using HikConsole.Infrastructure;

namespace HikConsole
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static void Main()
        {
            var container = AppBootstrapper.ConfigureIoc();
            var appConfig = container.Resolve<IHikConfig>().Config;
            var logger = container.Resolve<ILogger>();
            logger.Info(appConfig.ToString());

            var downloader = new HikDownloader(appConfig, container);

            if (appConfig.Mode == "Recurring")
            {
                using (Timer timer = new Timer(async (o) => await downloader.DownloadAsync(), null, 0, appConfig.Interval * 60 * 1000))
                {
                    WaitForExit();
                    downloader?.Cancel();
                }
            }
            else if (appConfig.Mode == "Fire-and-forget")
            {
                downloader.DownloadAsync().GetAwaiter().GetResult();
                Console.WriteLine("Press any key to quit.");
            }
            else
            {
                Console.WriteLine("Invalid config. Press any key to quit.");
            }
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

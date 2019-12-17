using System;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;
using HikConsole.Service;

namespace HikConsole
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        public static IServiceProvider Container { get; private set; }

        public static void Main()
        {
            var container = AppBootstrapper.ConfigureIoc();
            AppConfig appConfig = container.Resolve<IHikConfig>().Config;
            ILogger logger = container.Resolve<ILogger>();
            logger.Info(appConfig.ToString());

            var downloader = container.Resolve<HikDownloader>(new TypedParameter(typeof(AppConfig), appConfig));

            var result = downloader.DownloadAsync().GetAwaiter().GetResult();
            var jobResultSaver = new JobResultsSaver(appConfig.ConnectionString, result);
            jobResultSaver.SaveAsync().GetAwaiter().GetResult();

            if (appConfig.Mode == "Recurring")
            {
                logger.Info("Starting as service");
                ServiceStarter serviceStarter = new ServiceStarter();
                serviceStarter.StartService(appConfig, downloader);
            }

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

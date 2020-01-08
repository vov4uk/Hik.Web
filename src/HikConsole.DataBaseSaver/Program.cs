using Autofac;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.IO;
using System.Reflection;

namespace HikConsole.DataBaseSaver
{
    class Program
    {
        static void Main(string[] args)
        {
            var container = AppBootstrapper.ConfigureIoc();
            AppConfig appConfig = container.Resolve<IHikConfig>().Config;
            ILogger logger = container.Resolve<ILogger>();
            logger.Info(appConfig.ToString());

            var job = new HikJob
            {
                Started = DateTime.Now,
                JobType = nameof(HikDownloader),
            };

            var config = new ConfigurationBuilder()
                .SetBasePath(AssemblyDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var uowf = new UnitOfWorkFactory(config.GetConnectionString("HikConnectionString"));

            using (var unitOfWork = uowf.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                jobRepo.Add(job).GetAwaiter().GetResult();
                unitOfWork.SaveChangesAsync().GetAwaiter().GetResult();
            }

            var downloader = container.Resolve<HikDownloader>(new TypedParameter(typeof(AppConfig), appConfig));
            job.Finished = DateTime.Now;
            var result = downloader.DownloadAsync().GetAwaiter().GetResult();
            logger.Info("Save to DB...");
            var jobResultSaver = new JobService(uowf, job, result);
            jobResultSaver.SaveAsync().GetAwaiter().GetResult();
            logger.Info("Save to DB. Done!");

            if (appConfig.Mode == "Recurring")
            {
                logger.Info("Starting Recurring");
                var interval = appConfig.Interval * 60 * 1000;
                using (Timer timer = new Timer(async (o) => await downloader.DownloadAsync(), null, interval, interval))
                {
                    WaitForExit();
                    downloader?.Cancel();
                }
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

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}

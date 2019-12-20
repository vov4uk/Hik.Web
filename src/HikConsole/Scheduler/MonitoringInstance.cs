using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Helpers;
using ILogger = HikConsole.Abstraction.ILogger;

namespace HikConsole.Scheduler
{
    public class MonitoringInstance
    {
        private readonly ILogger logger;

        private readonly AppConfig configuration;

        private readonly IDeleteArchiving archiving;

        private readonly HikDownloader downloader;

        private readonly List<IDisposable> connections = new List<IDisposable>();

        private readonly IScheduler scheduler;

        public MonitoringInstance(IScheduler scheduler, AppConfig configuration, HikDownloader downloader, IDeleteArchiving archiving, ILogger logger)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => scheduler, scheduler);
            Guard.NotNull(() => archiving, archiving);
            this.configuration = configuration;
            this.archiving = archiving;
            this.downloader = downloader;
            this.scheduler = scheduler;
            this.logger = logger;
        }

        public bool Start()
        {
            if (this.configuration.RetentionPeriodDays.HasValue)
            {
                var archivingObservable = Observable.Empty<bool>()
                                                    .Delay(TimeSpan.FromDays(1), this.scheduler)
                                                    .Concat(Observable.FromAsync(this.Archiving, this.scheduler))
                                                    .Repeat()
                                                    .SubscribeOn(this.scheduler)
                                                    .Subscribe();
                this.connections.Add(archivingObservable);
            }

            var observableMonitor = Observable.Empty<bool>()
                                              .Delay(TimeSpan.FromMinutes(this.configuration.Interval), this.scheduler)
                                              .Concat(Observable.FromAsync(this.Download, this.scheduler))
                                              .Repeat()
                                              .SubscribeOn(this.scheduler)
                                              .Subscribe();

            this.connections.Add(observableMonitor);
            return true;
        }

        public void Stop()
        {
            this.downloader?.Cancel();
            foreach (var connection in this.connections)
            {
                connection.Dispose();
            }

            this.connections.Clear();
        }

        private async Task<bool> Archiving()
        {
            this.logger.Info("Archiving...");
            var result = this.archiving.Archive(new CameraConfig[] { }, TimeSpan.FromDays(this.configuration.RetentionPeriodDays.Value));
            this.logger.Info("Archiving. Done!");

            if (!string.IsNullOrEmpty(this.configuration.ConnectionString))
            {
                var jobResultSaver = new JobResultsSaver(this.configuration.ConnectionString, result, this.logger);
                await jobResultSaver.SaveAsync();
            }
            else
            {
                this.logger.Warn("ConnectionString not provided, nothing to save to DB");
            }

            return true;
        }

        private async Task<bool> Download()
        {
            this.logger.Info("Downloading...");
            var result = await this.downloader.DownloadAsync();
            this.logger.Info("Downloading. Done!");

            if (!string.IsNullOrEmpty(this.configuration.ConnectionString))
            {
                var jobResultSaver = new JobResultsSaver(this.configuration.ConnectionString, result, this.logger);
                await jobResultSaver.SaveAsync();
            }
            else
            {
                this.logger.Warn("ConnectionString not provided, nothing to save to DB");
            }

            return true;
        }
    }
}

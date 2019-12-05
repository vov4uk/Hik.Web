using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using HikApi.Data;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Helpers;

namespace HikConsole
{
    public class HikDownloader
    {
        private readonly AppConfig appConfig;
        private readonly IContainer container;
        private readonly ILogger logger;
        private readonly IEmailHelper emailHelper;
        private readonly IDirectoryHelper directoryHelper;
        private readonly CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private IHikClient client;

        public HikDownloader(AppConfig appConfig, IContainer container, int progressCheckPeriodMiliseconds = 5000)
        {
            this.appConfig = appConfig;
            this.container = container;
            this.logger = container.Resolve<ILogger>();
            this.emailHelper = container.Resolve<IEmailHelper>();
            this.directoryHelper = container.Resolve<IDirectoryHelper>();
            this.ProgressCheckPeriodMiliseconds = progressCheckPeriodMiliseconds;
        }

        public int ProgressCheckPeriodMiliseconds { get; }

        private CancellationToken Token => this.cancelTokenSource.Token;

        public async Task DownloadAsync()
        {
            DateTime start = DateTime.Now;

            this.logger.Info($"Start.");
            DateTime periodStart = start.AddHours(-1 * this.appConfig.ProcessingPeriodHours);
            DateTime periodEnd = start;

            foreach (var camera in this.appConfig.Cameras)
            {
                if (this.Token.IsCancellationRequested)
                {
                    this.LogWarnAndExit();
                    return;
                }

                await this.ProcessCameraAsync(camera, periodStart, periodEnd);
                this.logger.Info(new string('_', 40));
            }

            this.logger.Info($"Next execution at {start.AddMinutes(this.appConfig.Interval).ToString()}");
            string duration = (start - DateTime.Now).ToString("h'h 'm'm 's's'");
            this.logger.Info($"End. Duration  : {duration}");
        }

        public void Cancel()
        {
            this.cancelTokenSource.Cancel();
            this.logger.Warn("cancelTokenSource.cancel");
        }

        private void ForceExit()
        {
            if (this.client != null)
            {
                this.client.ForceExit();
                this.client = null;
                this.logger.Warn("ForceExit");
            }
            else
            {
                this.logger.Warn("ForceExit, no client found");
            }
        }

        private async Task ProcessCameraAsync(CameraConfig camera, DateTime periodStart, DateTime periodEnd)
        {
            try
            {
                this.client?.Logout();
                this.client = this.container.Resolve<IHikClient>(new TypedParameter(typeof(CameraConfig), camera));

                this.client.InitializeClient();

                if (this.Token.IsCancellationRequested)
                {
                    this.LogWarnAndExit();
                    return;
                }

                if (this.client.Login())
                {
                    if (this.Token.IsCancellationRequested)
                    {
                        this.LogWarnAndExit();
                        return;
                    }

                    this.logger.Info($"Login success!");
                    this.logger.Info(camera.ToString());
                    this.logger.Info($"Get videos from {periodStart.ToString()} to {periodEnd.ToString()}");

                    List<RemoteVideoFile> results = (await this.client.FindAsync(periodStart, periodEnd)).ToList();

                    this.logger.Info($"Searching finished");
                    this.logger.Info($"Found {results.Count.ToString()} files\r\n");

                    if (this.Token.IsCancellationRequested)
                    {
                        this.LogWarnAndExit();
                        return;
                    }

                    int i = 1;
                    foreach (var file in results)
                    {
                        await this.DownloadRemoteVideoFileAsync(file, i++, results.Count);
                    }

                    if (this.Token.IsCancellationRequested)
                    {
                        this.LogWarnAndExit();
                        return;
                    }

                    this.client.Logout();
                    this.client = null;

                    this.PrintStatistic(camera.DestinationFolder);
                }
                else
                {
                    this.logger.Warn("Unable to login");
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(camera.ToString(), ex);

                string msg = $"{camera.ToString()}\r\n{ex.ToString()}";
                this.emailHelper.SendEmail(this.appConfig.EmailConfig, msg);
                this.ForceExit();
            }
        }

        private void PrintStatistic(string destinationFolder)
        {
                string statisitcs = $@"
Directory Size : {Utils.FormatBytes(this.directoryHelper.DirSize(destinationFolder))}
Free space     : {Utils.FormatBytes(this.directoryHelper.GetTotalFreeSpace(destinationFolder))}";

                this.logger.Info(statisitcs);
        }

        private async Task DownloadRemoteVideoFileAsync(RemoteVideoFile file, int order, int count)
        {
            if (this.Token.IsCancellationRequested)
            {
                this.LogWarnAndExit();
                return;
            }

            this.logger.Info($"{order.ToString(),2}/{count.ToString()} : ");
            if (this.client.StartDownload(file))
            {
                do
                {
                    if (this.Token.IsCancellationRequested)
                    {
                        this.LogWarnAndExit();
                        return;
                    }

                    await Task.Delay(this.ProgressCheckPeriodMiliseconds);
                    this.client.UpdateProgress();
                }
                while (this.client.IsDownloading);
            }
        }

        private void LogWarnAndExit()
        {
            this.logger.Warn("CancellationRequested, ForceExit");
            this.ForceExit();
        }
    }
}

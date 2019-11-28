using System;
using System.Collections.Generic;
using System.Linq;
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
        private IHikClient client;

        public HikDownloader(AppConfig appConfig, IContainer container)
        {
            this.appConfig = appConfig;
            this.container = container;
            this.logger = container.Resolve<ILogger>();
        }

        public async Task DownloadAsync()
        {
            DateTime start = DateTime.Now;

            this.logger.Info($"Start.");
            DateTime periodStart = start.AddHours(-1 * this.appConfig.ProcessingPeriodHours);
            DateTime periodEnd = start;

            foreach (var camera in this.appConfig.Cameras)
            {
                await this.ProcessCameraAsync(camera, periodStart, periodEnd);
                this.logger.Info(new string('_', 40));
            }

            this.logger.Info($"Next execution at {start.AddMinutes(this.appConfig.Interval).ToString()}");
            string duration = (start - DateTime.Now).ToString("h'h 'm'm 's's'");
            this.logger.Info($"End. Duration  : {duration}");
        }

        public void ForceExit()
        {
            this.client?.ForceExit();
        }

        private async Task ProcessCameraAsync(CameraConfig camera, DateTime periodStart, DateTime periodEnd)
        {
            this.client?.Logout();
            this.client = this.container.Resolve<IHikClient>(new TypedParameter(typeof(CameraConfig), camera));

            try
            {
                this.client.InitializeClient();
                if (this.client.Login())
                {
                    this.logger.Info($"Login success!");
                    this.logger.Info(camera.ToString());
                    this.logger.Info($"Get videos from {periodStart.ToString()} to {periodEnd.ToString()}");

                    List<RemoteVideoFile> results = (await this.client.FindAsync(periodStart, periodEnd)).ToList();

                    this.logger.Info($"Searching finished");
                    this.logger.Info($"Found {results.Count.ToString()} files\r\n");

                    int i = 1;
                    foreach (var file in results)
                    {
                        await this.DownloadRemoteVideoFileAsync(file, i++, results.Count);
                    }

                    this.logger.Info(string.Empty);
                    this.client.Logout();
                    this.client = null;
                }

                this.PrintStatistic(camera);
            }
            catch (Exception ex)
            {
                this.logger.Error(camera.ToString(), ex);

                string msg = $"{camera.ToString()}\r\n{ex.ToString()}";
                EmailHelper.SendEmail(this.appConfig.EmailConfig, msg);
                this.client?.ForceExit();
            }
        }

        private void PrintStatistic(CameraConfig camera)
        {
            System.IO.FileInfo firstFile = Utils.GetOldestFile(camera.DestinationFolder);
            System.IO.FileInfo lastFile = Utils.GetNewestFile(camera.DestinationFolder);
            if (!string.IsNullOrEmpty(firstFile?.FullName) && !string.IsNullOrEmpty(lastFile?.FullName))
            {
                DateTime.TryParse(firstFile.Directory.Name, out var firstDate);
                DateTime.TryParse(lastFile.Directory.Name, out var lastDate);
                TimeSpan period = lastDate - firstDate;
                string statisitcs = $@"
Directory Size : {Utils.FormatBytes(Utils.DirSize(camera.DestinationFolder))}
Free space     : {Utils.FormatBytes(Utils.GetTotalFreeSpace(camera.DestinationFolder))}
Oldest File    : {firstFile.FullName.TrimStart(camera.DestinationFolder.ToCharArray())}
Newest File    : {lastFile.FullName.TrimStart(camera.DestinationFolder.ToCharArray())}
Period         : {Math.Floor(period.TotalDays).ToString()} days";

                this.logger.Info(statisitcs);
            }
        }

        private async Task DownloadRemoteVideoFileAsync(RemoteVideoFile file, int order, int count)
        {
            this.logger.Info($"{order.ToString(),2}/{count.ToString()} : ");
            if (this.client.StartDownload(file))
            {
                do
                {
                    await Task.Delay(5000);
                    this.client.UpdateProgress();
                }
                while (this.client.IsDownloading);
            }
        }
    }
}

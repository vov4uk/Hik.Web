using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private CancellationTokenSource cancelTokenSource;
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

        public async Task DownloadAsync()
        {
            using (this.cancelTokenSource = new CancellationTokenSource())
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                this.cancelTokenSource.Token.Register(() =>
                {
                    taskCompletionSource.TrySetCanceled();
                });

                try
                {
                    Task downloadTask = this.InternalDownload();
                    Task completedTask = await Task.WhenAny(downloadTask, taskCompletionSource.Task);

                    if (completedTask == downloadTask)
                    {
                        await downloadTask;
                        taskCompletionSource.TrySetResult(true);
                    }

                    await taskCompletionSource.Task;
                }
                catch (OperationCanceledException ex)
                {
                    this.logger.Error("Task was cancelled", ex);
                    this.ForceExit();
                }
                catch (Exception ex)
                {
                    StringBuilder msgBuilder = new StringBuilder("Exception happend : ");
                    if (ex.Data.Contains("Camera"))
                    {
                        msgBuilder.AppendLine(ex.Data["Camera"] as string);
                    }

                    msgBuilder.AppendLine(ex.ToString());
                    string msg = msgBuilder.ToString();

                    this.logger.Error(msg, ex);

                    this.emailHelper.SendEmail(this.appConfig.EmailConfig, msg);
                    this.ForceExit();
                }
            }
        }

        public void Cancel()
        {
            this.cancelTokenSource.Cancel();
            this.logger.Warn("Cancel signal was sent");
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

        private async Task InternalDownload()
        {
            DateTime appStart = DateTime.Now;

            this.logger.Info($"Start.");
            DateTime periodStart = appStart.AddHours(-1 * this.appConfig.ProcessingPeriodHours);

            foreach (var camera in this.appConfig.Cameras)
            {
                this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                await this.ProcessCameraAsync(camera, periodStart, appStart);
                this.logger.Info(new string('_', 40));
            }

            this.logger.Info($"Next execution at {appStart.AddMinutes(this.appConfig.Interval).ToString()}");
            string duration = (appStart - DateTime.Now).ToString("h'h 'm'm 's's'");
            this.logger.Info($"End. Duration  : {duration}");
        }

        private async Task ProcessCameraAsync(CameraConfig camera, DateTime periodStart, DateTime periodEnd)
        {
            try
            {
                this.client?.Logout();
                this.client = this.container.Resolve<IHikClient>(new TypedParameter(typeof(CameraConfig), camera));

                this.client.InitializeClient();
                this.cancelTokenSource.Token.ThrowIfCancellationRequested();

                if (this.client.Login())
                {
                    this.logger.Info($"Login success!");
                    this.logger.Info(camera.ToString());
                    this.logger.Info($"Get videos from {periodStart.ToString()} to {periodEnd.ToString()}");

                    this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                    List<RemoteVideoFile> results = (await this.client.FindAsync(periodStart, periodEnd)).ToList();

                    this.logger.Info($"Searching finished");
                    this.logger.Info($"Found {results.Count.ToString()} files\r\n");

                    int i = 1;
                    foreach (var file in results)
                    {
                        this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                        await this.DownloadRemoteVideoFileAsync(file, i++, results.Count);
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
                ex.Data.Add("Camera", camera.ToString());
                throw;
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
            this.logger.Info($"{order.ToString(),2}/{count.ToString()} : ");
            if (this.client.StartDownload(file))
            {
                do
                {
                    await Task.Delay(this.ProgressCheckPeriodMiliseconds);
                    this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                    this.client.UpdateProgress();
                }
                while (this.client.IsDownloading);
            }
        }
    }
}

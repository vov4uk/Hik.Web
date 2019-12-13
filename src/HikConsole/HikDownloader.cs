using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HikApi.Data;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Helpers;

namespace HikConsole
{
    public class HikDownloader
    {
        private const string DurationFormat = "h'h 'm'm 's's'";
        private readonly AppConfig appConfig;
        private readonly ILogger logger;
        private readonly IEmailHelper emailHelper;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IHikClientFactory clientFactory;
        private readonly IProgressBarFactory progressFactory;
        private CancellationTokenSource cancelTokenSource;
        private IHikClient client;
        private DateTime? lastRun;

        public HikDownloader(
            AppConfig appConfig,
            ILogger logger,
            IEmailHelper emailHelper,
            IDirectoryHelper directoryHelper,
            IHikClientFactory clientFactory,
            IProgressBarFactory progressFactory,
            int progressCheckPeriodMilliseconds = 5000)
        {
            this.appConfig = appConfig;

            this.logger = logger;
            this.emailHelper = emailHelper;
            this.directoryHelper = directoryHelper;
            this.clientFactory = clientFactory;
            this.progressFactory = progressFactory;
            this.ProgressCheckPeriodMilliseconds = progressCheckPeriodMilliseconds;
        }

        public int ProgressCheckPeriodMilliseconds { get; }

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
                    this.HandleException(ex);
                }
            }

            this.cancelTokenSource = null;
        }

        public void Cancel()
        {
            if (this.cancelTokenSource != null && this.cancelTokenSource.Token.CanBeCanceled)
            {
                this.cancelTokenSource.Cancel();
                this.logger.Warn("Cancel signal was sent");
            }
            else
            {
                this.logger.Warn("Nothing to Cancel");
            }
        }

        private void ForceExit()
        {
            this.client?.ForceExit();
            this.client = null;

            this.cancelTokenSource?.Dispose();
            this.cancelTokenSource = null;
        }

        private async Task InternalDownload()
        {
            DateTime appStart = DateTime.Now;

            this.logger.Info($"Start.");
            DateTime periodStart = this.lastRun ?? appStart.AddHours(-1 * this.appConfig.ProcessingPeriodHours);

            foreach (var camera in this.appConfig.Cameras)
            {
                this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                await this.ProcessCameraAsync(camera, periodStart, appStart);
            }

            string duration = (DateTime.Now - appStart).ToString(DurationFormat);
            this.logger.Info($"End. Duration  : {duration}");
            this.logger.Info($"Next execution at {appStart.AddMinutes(this.appConfig.Interval).ToString()}");
            this.lastRun = appStart;
        }

        private async Task ProcessCameraAsync(CameraConfig camera, DateTime periodStart, DateTime periodEnd)
        {
            try
            {
                using (this.client = this.clientFactory.Create(camera))
                {
                    this.client.InitializeClient();
                    this.cancelTokenSource.Token.ThrowIfCancellationRequested();

                    if (this.client.Login())
                    {
                        this.client.CheckHardDriveStatus();

                        this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                        List<RemoteVideoFile> videos = (await this.client.FindVideosAsync(periodStart, periodEnd)).SkipLast(1).ToList();

                        string resultCountString = videos.Count.ToString();
                        this.logger.Info($"Video searching finished. Found {resultCountString} files");

                        int j = 1;
                        foreach (RemoteVideoFile video in videos)
                        {
                            this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                            this.logger.Info($"{(j++).ToString(),2}/{resultCountString} : ");
                            await this.DownloadRemoteVideoFileAsync(video);
                        }

                        List<RemotePhotoFile> photos = (await this.client.FindPhotosAsync(periodStart, periodEnd)).ToList();
                        resultCountString = photos.Count.ToString();

                        this.logger.Info("Photos searching finished.");
                        this.logger.Info($"Found {resultCountString} photos");

                        var photoDownloadResults = new Dictionary<bool, int>
                        {
                            { false, 0 },
                            { true, 0 },
                        };

                        j = 0;
                        using (var progressBar = this.progressFactory.Create())
                        {
                            foreach (RemotePhotoFile photo in photos)
                            {
                                this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                                bool isDownloaded = this.client.PhotoDownload(photo);
                                photoDownloadResults[isDownloaded]++;
                                j++;
                                progressBar.Report((double)j / photos.Count);
                            }
                        }

                        this.logger.Info($"Exist {photoDownloadResults[false]} photos");
                        this.logger.Info($"Downloaded {photoDownloadResults[true]} photos");

                        this.PrintStatistic(camera.DestinationFolder);
                    }
                    else
                    {
                        this.logger.Warn("Unable to login");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                ex.Data.Add("Camera", camera.ToString());
                throw;
            }
            catch (Exception ex)
            {
                this.HandleException(ex);
            }
        }

        private void PrintStatistic(string destinationFolder)
        {
            StringBuilder statisticsSb = new StringBuilder();
            statisticsSb.AppendLine();
            statisticsSb.AppendLine();
            statisticsSb.AppendLine($"Directory Size : {Utils.FormatBytes(this.directoryHelper.DirSize(destinationFolder))}");
            statisticsSb.AppendLine($"Free space     : {Utils.FormatBytes(this.directoryHelper.GetTotalFreeSpace(destinationFolder))}");
            statisticsSb.AppendLine(new string('_', 40)); // separator

            this.logger.Info(statisticsSb.ToString());
        }

        private async Task DownloadRemoteVideoFileAsync(RemoteVideoFile file)
        {
            if (this.client.StartVideoDownload(file))
            {
                DateTime downloadStarted = DateTime.Now;
                do
                {
                    await Task.Delay(this.ProgressCheckPeriodMilliseconds);
                    this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                    this.client.UpdateVideoProgress();
                }
                while (this.client.IsDownloading);

                TimeSpan duration = DateTime.Now - downloadStarted;
                this.logger.Info($"Download duration {duration.ToString(DurationFormat)}, avg speed {Utils.FormatBytes((long)(file.Size / duration.TotalSeconds))}/s");
            }
        }

        private string GetExceptionMessage(Exception ex)
        {
            StringBuilder msgBuilder = new StringBuilder("Exception happened : ");
            if (ex.Data.Contains("Camera"))
            {
                msgBuilder.AppendLine(ex.Data["Camera"] as string);
            }

            msgBuilder.AppendLine(ex.ToString());
            return msgBuilder.ToString();
        }

        private void HandleException(Exception ex)
        {
            string msg = this.GetExceptionMessage(ex);

            this.logger.Error(msg, ex);

            this.emailHelper.SendEmail(this.appConfig.EmailConfig, msg);
            this.ForceExit();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HikApi.Data;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.DataAccess.Data;
using HikConsole.Helpers;

namespace HikConsole.Scheduler
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
            IProgressBarFactory progressFactory)
        {
            this.appConfig = appConfig;

            this.logger = logger;
            this.emailHelper = emailHelper;
            this.directoryHelper = directoryHelper;
            this.clientFactory = clientFactory;
            this.progressFactory = progressFactory;
        }

        public int ProgressCheckPeriodMilliseconds { get; set; } = 5000;

        public AppConfig Config { get => this.appConfig; }

        public async Task<JobResult> DownloadAsync()
        {
            JobResult jobResult = null;
            using (this.cancelTokenSource = new CancellationTokenSource())
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                this.cancelTokenSource.Token.Register(() =>
                {
                    taskCompletionSource.TrySetCanceled();
                });

                try
                {
                    Task<JobResult> downloadTask = this.InternalDownload();
                    Task completedTask = await Task.WhenAny(downloadTask, taskCompletionSource.Task);

                    if (completedTask == downloadTask)
                    {
                        jobResult = await downloadTask;
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
            return jobResult;
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
        }

        private async Task<JobResult> InternalDownload()
        {
            DateTime appStart = DateTime.Now;

            this.logger.Info($"Start.");
            DateTime periodStart = this.lastRun ?? appStart.AddHours(-1 * this.appConfig.ProcessingPeriodHours);

            var job = new Job { PeriodStart = periodStart, PeriodEnd = appStart, Started = appStart, JobType = nameof(HikDownloader) };
            var jobResult = new JobResult(job);
            bool failed = false;

            foreach (var camera in this.appConfig.Cameras)
            {
                var result = await this.ProcessCameraAsync(camera, periodStart, appStart);
                jobResult.CameraResults[camera.Alias] = result;

                if (!failed)
                {
                    failed = result.Failed;
                }
            }

            string duration = (DateTime.Now - appStart).ToString(DurationFormat);
            this.logger.Info($"End. Duration  : {duration}");
            this.logger.Info($"Next execution at {appStart.AddMinutes(this.appConfig.Interval).ToString()}");
            this.lastRun = failed ? periodStart : appStart;

            job.Finished = DateTime.Now;
            return jobResult;
        }

        private async Task<CameraResult> ProcessCameraAsync(CameraConfig cameraConf, DateTime periodStart, DateTime periodEnd)
        {
            var result = new CameraResult(cameraConf);
            try
            {
                using (this.client = this.clientFactory.Create(cameraConf))
                {
                    this.client.InitializeClient();
                    this.cancelTokenSource.Token.ThrowIfCancellationRequested();

                    if (this.client.Login())
                    {
                        this.CheckClientHardDriveStatus(result);

                        this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                        var videos = await this.DownloadVideos(periodStart, periodEnd);
                        result.DownloadedVideos.AddRange(videos);

                        if (cameraConf.DownloadPhotos)
                        {
                            var photos = await this.DownloadPhotos(periodStart, periodEnd);
                            result.DownloadedPhotos.AddRange(photos);
                        }

                        this.PrintStatistic(cameraConf.DestinationFolder);
                    }
                    else
                    {
                        this.logger.Warn("Unable to login");
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                ex.Data.Add("Camera", cameraConf.ToString());
                throw;
            }
            catch (Exception ex)
            {
                result.Failed = true;
                ex.Data.Add("Camera", cameraConf.ToString());
                this.HandleException(ex);
            }

            return result;
        }

        private async Task<IEnumerable<Photo>> DownloadPhotos(
            DateTime periodStart,
            DateTime periodEnd)
        {
            var photos = await this.GetRemotePhotosFilesList(periodStart, periodEnd);

            return this.DownloadPhotosFromClient(photos);
        }

        private async Task<IEnumerable<Video>> DownloadVideos(
            DateTime periodStart,
            DateTime periodEnd)
        {
            var videos = await this.GetRemoteVideoFilesList(periodStart, periodEnd);

            return await this.DownloadVideoFilesFromClient(videos);
        }

        private List<Photo> DownloadPhotosFromClient(List<RemotePhotoFile> photos)
        {
            var photoDownloadResults = new Dictionary<bool, int>
            {
                { false, 0 },
                { true, 0 },
            };

            int j = 0;
            var photosFromClient = new List<Photo>();
            using (var progressBar = this.progressFactory.Create())
            {
                foreach (RemotePhotoFile photo in photos)
                {
                    DateTime start = DateTime.Now;
                    this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                    bool isDownloaded = this.client.PhotoDownload(photo);
                    DateTime finish = DateTime.Now;
                    photoDownloadResults[isDownloaded]++;
                    j++;
                    progressBar.Report((double)j / photos.Count);

                    if (isDownloaded)
                    {
                        photosFromClient.Add(new Photo
                        {
                            Size = photo.Size,
                            Name = photo.Name,
                            DateTaken = photo.Date,
                            DownloadStartTime = start,
                            DownloadStopTime = finish,
                        });
                    }
                }
            }

            this.logger.Info($"Exist {photoDownloadResults[false]} photos");
            this.logger.Info($"Downloaded {photoDownloadResults[true]} photos");
            return photosFromClient;
        }

        private async Task<List<RemotePhotoFile>> GetRemotePhotosFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<RemotePhotoFile> photos = (await this.client.FindPhotosAsync(periodStart, periodEnd)).ToList();
            var resultCountString = photos.Count.ToString();

            this.logger.Info("Photos searching finished.");
            this.logger.Info($"Found {resultCountString} photos");
            return photos;
        }

        private async Task<IEnumerable<Video>> DownloadVideoFilesFromClient(List<RemoteVideoFile> videos)
        {
            int j = 1;
            var videoResult = new List<Video>();
            foreach (RemoteVideoFile video in videos)
            {
                this.cancelTokenSource.Token.ThrowIfCancellationRequested();
                this.logger.Info($"{(j++).ToString(),2}/{videos.Count} : ");
                var videoDownloadResult = await this.DownloadRemoteVideoFileAsync(video);
                if (videoDownloadResult != null)
                {
                    videoResult.Add(videoDownloadResult);
                }
            }

            return videoResult;
        }

        private async Task<List<RemoteVideoFile>> GetRemoteVideoFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<RemoteVideoFile> videos = (await this.client.FindVideosAsync(periodStart, periodEnd)).SkipLast(1).ToList();

            this.logger.Info($"Video searching finished. Found {videos.Count} files");
            return videos;
        }

        private void CheckClientHardDriveStatus(CameraResult cameraResult)
        {
            var status = this.client.CheckHardDriveStatus();

            cameraResult.HardDriveStatus = new HardDriveStatus
            {
                Capacity = status.Capacity,
                FreeSpace = status.FreeSpace,
                PictureCapacity = status.PictureCapacity,
                FreePictureSpace = status.FreePictureSpace,
                HDAttr = status.HDAttr,
                HdStatus = status.HdStatus,
                HDType = status.HDType,
                Recycling = status.Recycling,
            };
            this.logger.Info(status.ToString());

            if (status.IsErrorStatus)
            {
                throw new InvalidOperationException("HD error");
            }
        }

        private void PrintStatistic(string destinationFolder)
        {
            StringBuilder statisticsSb = new StringBuilder();
            statisticsSb.AppendLine();
            statisticsSb.AppendLine();
            statisticsSb.AppendLine($"{"Directory Size",-24}: {Utils.FormatBytes(this.directoryHelper.DirSize(destinationFolder))}");
            statisticsSb.AppendLine($"{"Free space",-24}: {Utils.FormatBytes(this.directoryHelper.GetTotalFreeSpace(destinationFolder))}");
            statisticsSb.AppendLine(new string('_', 40)); // separator

            this.logger.Info(statisticsSb.ToString());
        }

        private async Task<Video> DownloadRemoteVideoFileAsync(RemoteVideoFile file)
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

                return new Video
                {
                    Size = file.Size,
                    Name = file.Name,
                    StartTime = file.StartTime,
                    StopTime = file.StopTime,
                    DownloadStartTime = downloadStarted,
                    DownloadStopTime = DateTime.Now,
                };
            }

            return default(Video);
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

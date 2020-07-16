using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using HikApi.Data;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.DTO;
using HikConsole.DTO.Contracts;
using HikConsole.Events;
using HikConsole.Helpers;
using NLog;

namespace HikConsole.Scheduler
{
    public class HikDownloader
    {
        private const string DurationFormat = "h'h 'm'm 's's'";
        private readonly IHikConfig hikConfig;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IHikClientFactory clientFactory;
        private readonly IMapper mapper;
        private ILogger logger = LogManager.GetCurrentClassLogger();
        private CancellationTokenSource cancelTokenSource;
        private IHikClient client;
        private Dictionary<string, DateTime?> lastRunList;

        public HikDownloader(
            IHikConfig hikConfig,
            IDirectoryHelper directoryHelper,
            IHikClientFactory clientFactory,
            IMapper mapper)
        {
            this.hikConfig = hikConfig;
            this.directoryHelper = directoryHelper;
            this.clientFactory = clientFactory;
            this.mapper = mapper;
        }

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public event EventHandler<VideoEventArgs> VideoDownloaded;

        public int ProgressCheckPeriodMilliseconds { get; set; } = 5000;

        public async Task<JobResult> DownloadAsync(string configFileName)
        {
            var appConfig = this.hikConfig.GetConfig(configFileName);

            JobResult jobResult = null;
            using (this.cancelTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(appConfig.JobTimeout)))
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                this.cancelTokenSource.Token.Register(() =>
                {
                    taskCompletionSource.TrySetCanceled();
                });

                try
                {
                    Task<JobResult> downloadTask = this.InternalDownload(appConfig);
                    Task completedTask = await Task.WhenAny(downloadTask, taskCompletionSource.Task);

                    if (completedTask == downloadTask)
                    {
                        jobResult = await downloadTask;
                        taskCompletionSource.TrySetResult(true);
                    }

                    await taskCompletionSource.Task;
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
            if (this.cancelTokenSource != null
                && this.cancelTokenSource.Token != null
                && this.cancelTokenSource.Token.CanBeCanceled)
            {
                this.cancelTokenSource.Cancel();
                this.logger.Warn("Cancel signal was sent");
            }
            else
            {
                this.logger.Warn("Nothing to Cancel");
            }
        }

        public void SetLastSyncDates(Dictionary<string, DateTime?> lastRun)
        {
            this.lastRunList = lastRun;
        }

        protected virtual void OnExceptionFired(ExceptionEventArgs e)
        {
            this.ExceptionFired?.Invoke(this, e);
        }

        protected virtual void OnVideoDownloaded(VideoEventArgs e)
        {
            this.VideoDownloaded?.Invoke(this, e);
        }

        private void ForceExit()
        {
            this.client?.ForceExit();
            this.client = null;
        }

        private async Task<JobResult> InternalDownload(AppConfig appConfig)
        {
            DateTime appStart = DateTime.Now;

            this.logger.Info($"Start.");
            var jobResult = new JobResult { PeriodEnd = appStart };

            bool failed = false;

            foreach (var camera in appConfig.Cameras)
            {
                DateTime? lastRunDate = null;

                this.lastRunList?.TryGetValue(camera.Alias, out lastRunDate);
                DateTime periodStart = lastRunDate?.AddMinutes(-1) ?? appStart.AddHours(-1 * appConfig.ProcessingPeriodHours);

                jobResult.PeriodStart = periodStart;

                var result = await this.ProcessCameraAsync(camera, periodStart, appStart);
                jobResult.CameraResults[camera.Alias] = result;

                if (!failed)
                {
                    failed = result.Failed;
                }
            }

            string duration = (DateTime.Now - appStart).ToString(DurationFormat);
            this.logger.Info($"End. Duration  : {duration}");
            return jobResult;
        }

        private async Task<CameraResult> ProcessCameraAsync(CameraConfig cameraConf, DateTime periodStart, DateTime periodEnd)
        {
            var cameraDto = this.mapper.Map<CameraDTO>(cameraConf);
            var result = new CameraResult(cameraDto);
            try
            {
                using (this.client = this.clientFactory.Create(cameraConf))
                {
                    this.client.InitializeClient();
                    this.ThrowIfCancellationRequested();

                    if (this.client.Login())
                    {
                        this.CheckClientHardDriveStatus(result);

                        this.ThrowIfCancellationRequested();

                        var remoteVideoFiles = await this.GetRemoteVideoFilesList(periodStart, periodEnd);

                        int j = 1;
                        foreach (RemoteVideoFile video in remoteVideoFiles)
                        {
                            this.ThrowIfCancellationRequested();
                            this.logger.Info($"{j++,2}/{remoteVideoFiles.Count} : ");
                            var videoDownloadResult = await this.DownloadRemoteVideoFileAsync(video);
                            if (videoDownloadResult != null)
                            {
                                this.OnVideoDownloaded(new VideoEventArgs(videoDownloadResult, cameraDto));
                                result.DownloadedVideos.Add(videoDownloadResult);
                            }
                        }

                        if (cameraConf.DownloadPhotos)
                        {
                            var remotePhotoFiles = await this.GetRemotePhotosFilesList(periodStart, periodEnd);

                            var downloadedPhotos = this.DownloadPhotosFromClient(remotePhotoFiles);
                            result.DownloadedPhotos.AddRange(downloadedPhotos);
                        }

                        this.PrintStatistic(cameraConf.DestinationFolder);
                    }
                    else
                    {
                        this.logger.Warn("Unable to login");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Failed = true;
                ex.Data.Add("Camera", cameraConf);
                this.HandleException(ex);
            }

            return result;
        }

        private IReadOnlyCollection<PhotoDTO> DownloadPhotosFromClient(IReadOnlyCollection<RemotePhotoFile> photos)
        {
            var photoDownloadResults = new Dictionary<bool, int>
            {
                { false, 0 },
                { true, 0 },
            };

            int j = 0;
            var photosFromClient = new List<PhotoDTO>();

            foreach (RemotePhotoFile photo in photos)
            {
                DateTime start = DateTime.Now;
                this.ThrowIfCancellationRequested();
                bool isDownloaded = this.client.PhotoDownload(photo);
                DateTime finish = DateTime.Now;
                photoDownloadResults[isDownloaded]++;
                j++;

                // TODO report downloading progress via event
                if (isDownloaded)
                {
                    var photoDto = this.mapper.Map<PhotoDTO>(photo);
                    photoDto.DownloadStartTime = start;
                    photoDto.DownloadStopTime = finish;

                    photosFromClient.Add(photoDto);
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

        private async Task<IReadOnlyCollection<RemoteVideoFile>> GetRemoteVideoFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<RemoteVideoFile> videos = (await this.client.FindVideosAsync(periodStart, periodEnd)).SkipLast(1).ToList();

            this.logger.Info($"Video searching finished. Found {videos.Count} files");
            return videos;
        }

        private void CheckClientHardDriveStatus(CameraResult cameraResult)
        {
            var status = this.client.CheckHardDriveStatus();

            cameraResult.HardDriveStatus = this.mapper.Map<HardDriveStatusDTO>(status);
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

        private async Task<VideoDTO> DownloadRemoteVideoFileAsync(RemoteVideoFile file)
        {
            if (this.client.StartVideoDownload(file))
            {
                DateTime downloadStarted = DateTime.Now;
                do
                {
                    await Task.Delay(this.ProgressCheckPeriodMilliseconds);
                    this.ThrowIfCancellationRequested();
                    this.client.UpdateVideoProgress();
                }
                while (this.client.IsDownloading);

                TimeSpan duration = DateTime.Now - downloadStarted;
                this.logger.Info($"Download duration {duration.ToString(DurationFormat)}, avg speed {Utils.FormatBytes((long)(file.Size / duration.TotalSeconds))}/s");

                VideoDTO result = this.mapper.Map<VideoDTO>(file);
                result.DownloadStartTime = downloadStarted;
                result.DownloadStopTime = DateTime.Now;
                return result;
            }

            return default;
        }

        private string GetExceptionMessage(Exception ex)
        {
            StringBuilder msgBuilder = new StringBuilder();
            if (ex.Data.Contains("Camera") && ex.Data["Camera"] is CameraConfig)
            {
                msgBuilder.AppendLine((ex.Data["Camera"] as CameraConfig).ToString());
            }

            msgBuilder.AppendLine(ex.ToString());
            return msgBuilder.ToString();
        }

        private void HandleException(Exception ex)
        {
            string msg = this.GetExceptionMessage(ex);

            this.logger.Error(ex, msg);

            this.OnExceptionFired(new ExceptionEventArgs(ex));

            this.ForceExit();
        }

        private void ThrowIfCancellationRequested()
        {
            if (this.cancelTokenSource != null && this.cancelTokenSource.Token != null)
            {
                this.cancelTokenSource.Token.ThrowIfCancellationRequested();
            }
            else
            {
                throw new OperationCanceledException();
            }
        }
    }
}

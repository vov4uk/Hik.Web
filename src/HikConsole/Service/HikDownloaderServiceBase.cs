using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using HikApi.Abstraction;
using HikConsole.Abstraction;
using HikConsole.DTO.Config;
using HikConsole.DTO.Contracts;
using HikConsole.Events;
using HikConsole.Helpers;
using NLog;

namespace HikConsole.Service
{
    public abstract class HikDownloaderServiceBase<T> : IRecurrentJob<T>
        where T : MediaFileBase
    {
        protected const string DurationFormat = "h'h 'm'm 's's'";
        protected const int JobTimeout = 30;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IHikClientFactory clientFactory;
        private CancellationTokenSource cancelTokenSource;

        public HikDownloaderServiceBase(
            IDirectoryHelper directoryHelper,
            IHikClientFactory clientFactory,
            IMapper mapper)
        {
            this.directoryHelper = directoryHelper;
            this.clientFactory = clientFactory;
            this.Mapper = mapper;
        }

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public event EventHandler<FileDownloadedEventArgs> FileDownloaded;

        protected IMapper Mapper { get; private set; }

        protected ILogger Logger
        {
            get { return LogManager.GetCurrentClassLogger(); }
        }

        protected IHikClient Client { get; private set; }

        public async Task<IReadOnlyCollection<T>> ExecuteAsync(CameraConfig config, DateTime from, DateTime to)
        {
            IReadOnlyCollection<T> jobResult = null;
            using (this.cancelTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(JobTimeout)))
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                this.cancelTokenSource.Token.Register(() =>
                {
                    taskCompletionSource.TrySetCanceled();
                });

                try
                {
                    Task<IReadOnlyCollection<T>> downloadTask = this.InternalDownload(config, from, to);
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
                && this.cancelTokenSource.Token.CanBeCanceled)
            {
                this.cancelTokenSource.Cancel();
                this.Logger.Warn("Cancel signal was sent");
            }
            else
            {
                this.Logger.Warn("Nothing to Cancel");
            }
        }

        public abstract Task<IReadOnlyCollection<IRemoteFile>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd);

        public abstract Task<IReadOnlyCollection<T>> DownloadFilesFromClientAsync(IReadOnlyCollection<IRemoteFile> remoteFiles);

        protected virtual void OnExceptionFired(ExceptionEventArgs e)
        {
            this.ExceptionFired?.Invoke(this, e);
        }

        protected virtual void OnFileDownloaded(FileDownloadedEventArgs e)
        {
            this.FileDownloaded?.Invoke(this, e);
        }

        protected void ThrowIfCancellationRequested()
        {
            if (this.cancelTokenSource != null)
            {
                this.cancelTokenSource.Token.ThrowIfCancellationRequested();
            }
            else
            {
                throw new OperationCanceledException();
            }
        }

        private void ForceExit()
        {
            this.Client?.ForceExit();
            this.Client = null;
        }

        private async Task<IReadOnlyCollection<T>> InternalDownload(CameraConfig config, DateTime from, DateTime to)
        {
            DateTime appStart = DateTime.Now;

            this.Logger.Info($"Internal download...");

            var result = await this.ProcessCameraAsync(config, from, to);

            this.PrintStatistic(config?.DestinationFolder);
            string duration = (DateTime.Now - appStart).ToString(DurationFormat);
            this.Logger.Info($"Internal download. Done. Duration  : {duration}");
            return result;
        }

        private async Task<IReadOnlyCollection<T>> ProcessCameraAsync(CameraConfig cameraConf, DateTime periodStart, DateTime periodEnd)
        {
            var result = new List<T>();
            try
            {
                using (this.Client = this.clientFactory.Create(cameraConf))
                {
                    this.Client.InitializeClient();
                    this.ThrowIfCancellationRequested();

                    if (this.Client.Login())
                    {
                        this.CheckClientHardDriveStatus();

                        this.ThrowIfCancellationRequested();
                        this.Logger.Info($"Reading remote files...");
                        var remoteFiles = await this.GetRemoteFilesList(periodStart, periodEnd);
                        this.Logger.Info($"Reading remote files. Done");

                        this.Logger.Info($"Downloading files...");
                        var downloadedFiles = await this.DownloadFilesFromClientAsync(remoteFiles);
                        result.AddRange(downloadedFiles);
                        this.Logger.Info($"Downloading files. Done");
                    }
                    else
                    {
                        this.Logger.Warn("Unable to login");
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("Camera", cameraConf);
                this.HandleException(ex);
            }

            return result;
        }

        private void CheckClientHardDriveStatus()
        {
            var status = this.Client.CheckHardDriveStatus();

            this.Logger.Info(status.ToString());

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

            this.Logger.Info(statisticsSb.ToString());
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

            this.Logger.Error(ex, msg);

            this.OnExceptionFired(new ExceptionEventArgs(ex));

            this.ForceExit();
        }
    }
}

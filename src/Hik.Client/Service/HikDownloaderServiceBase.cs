using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using NLog;

namespace Hik.Client.Service
{
    public abstract class HikDownloaderServiceBase<T> : IRecurrentJob<T>
        where T : MediaFileDTO
    {
        protected const int JobTimeout = 30;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IClientFactory clientFactory;
        private CancellationTokenSource cancelTokenSource;

        public HikDownloaderServiceBase(
            IDirectoryHelper directoryHelper,
            IClientFactory clientFactory,
            IMapper mapper)
        {
            this.directoryHelper = directoryHelper;
            this.clientFactory = clientFactory;
            Mapper = mapper;
        }

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public event EventHandler<FileDownloadedEventArgs> FileDownloaded;

        protected IMapper Mapper { get; private set; }

        protected ILogger Logger
        {
            get { return LogManager.GetCurrentClassLogger(); }
        }

        protected IClient Client { get; private set; }

        public async Task<IReadOnlyCollection<T>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to)
        {
            IReadOnlyCollection<T> jobResult = null;
            using (cancelTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(JobTimeout)))
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                cancelTokenSource.Token.Register(() =>
                {
                    taskCompletionSource.TrySetCanceled();
                });

                try
                {
                    Task<IReadOnlyCollection<T>> downloadTask = InternalDownload(config as CameraConfig, from, to);
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
                    HandleException(ex);
                }
            }

            cancelTokenSource = null;
            return jobResult;
        }

        public void Cancel()
        {
            if (cancelTokenSource != null
                && cancelTokenSource.Token.CanBeCanceled)
            {
                cancelTokenSource.Cancel();
                Logger.Warn("Cancel signal was sent");
            }
            else
            {
                Logger.Warn("Nothing to Cancel");
            }
        }

        public abstract Task<IReadOnlyCollection<MediaFileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd);

        public abstract Task<IReadOnlyCollection<T>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDTO> remoteFiles, CancellationToken token);

        protected virtual void OnExceptionFired(ExceptionEventArgs e)
        {
            ExceptionFired?.Invoke(this, e);
        }

        protected virtual void OnFileDownloaded(FileDownloadedEventArgs e)
        {
            FileDownloaded?.Invoke(this, e);
        }

        protected void ThrowIfCancellationRequested()
        {
            if (cancelTokenSource != null)
            {
                cancelTokenSource.Token.ThrowIfCancellationRequested();
            }
            else
            {
                throw new OperationCanceledException();
            }
        }

        private void ForceExit()
        {
            Client?.ForceExit();
            Client = null;
        }

        private async Task<IReadOnlyCollection<T>> InternalDownload(CameraConfig config, DateTime from, DateTime to)
        {
            DateTime appStart = DateTime.Now;

            Logger.Info($"Internal download...");

            var result = await ProcessCameraAsync(config, from, to);
            if (result.Count > 0)
            {
                PrintStatistic(config?.DestinationFolder);
                var duration = (DateTime.Now - appStart).TotalSeconds;
                Logger.Info($"Internal download. Done. Duration  : {duration.FormatSeconds()}");
            }
            else
            {
               Logger.Warn($"{config.Alias}, {from} - {to} : No files downloaded");
            }

            return result;
        }

        private async Task<IReadOnlyCollection<T>> ProcessCameraAsync(CameraConfig cameraConf, DateTime periodStart, DateTime periodEnd)
        {
            var result = new List<T>();
            try
            {
                using (Client = clientFactory.Create(cameraConf))
                {
                    Client.InitializeClient();
                    ThrowIfCancellationRequested();

                    if (Client.Login())
                    {
                        ThrowIfCancellationRequested();
                        Logger.Info($"Reading remote files...");
                        var remoteFiles = await GetRemoteFilesList(periodStart, periodEnd);
                        Logger.Info($"Reading remote files. Done");

                        Logger.Info($"Downloading files...");
                        var downloadedFiles = await DownloadFilesFromClientAsync(remoteFiles, cancelTokenSource?.Token ?? CancellationToken.None);
                        result.AddRange(downloadedFiles);
                        Logger.Info($"Downloading files. Done");
                    }
                    else
                    {
                        Logger.Warn("Unable to login");
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Data.Add("Camera", cameraConf);
                HandleException(ex);
            }

            return result;
        }

        private void PrintStatistic(string destinationFolder)
        {
            StringBuilder statisticsSb = new StringBuilder();
            statisticsSb.AppendLine();
            statisticsSb.AppendLine();
            statisticsSb.AppendLine($"{"Directory Size",-24}: {Utils.FormatBytes(directoryHelper.DirSize(destinationFolder))}");
            statisticsSb.AppendLine($"{"Free space",-24}: {Utils.FormatBytes(directoryHelper.GetTotalFreeSpace(destinationFolder))}");
            statisticsSb.AppendLine(new string('_', 40)); // separator

            Logger.Info(statisticsSb.ToString());
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
            string msg = GetExceptionMessage(ex);

            Logger.Error(ex, msg);

            OnExceptionFired(new ExceptionEventArgs(ex));

            ForceExit();
        }
    }
}

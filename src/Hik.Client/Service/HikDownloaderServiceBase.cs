using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public abstract class HikDownloaderServiceBase<T> : RecurrentJobBase<T>
        where T : MediaFileDTO
    {
        protected const int JobTimeout = 30;
        private readonly IClientFactory clientFactory;
        private CancellationTokenSource cancelTokenSource;

        protected HikDownloaderServiceBase(
            IDirectoryHelper directoryHelper,
            IClientFactory clientFactory)
            : base(directoryHelper)
        {
            this.clientFactory = clientFactory;
        }

        public event EventHandler<FileDownloadedEventArgs> FileDownloaded;

        protected IClient Client { get; private set; }

        public void Cancel()
        {
            if (cancelTokenSource != null
                && cancelTokenSource.Token.CanBeCanceled)
            {
                cancelTokenSource.Cancel();
                logger.Warn("Cancel signal was sent");
            }
            else
            {
                logger.Warn("Nothing to Cancel");
            }
        }

        public abstract Task<IReadOnlyCollection<MediaFileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd);

        public abstract Task<IReadOnlyCollection<T>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDTO> remoteFiles, CancellationToken token);

        protected override async Task<IReadOnlyCollection<T>> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            IReadOnlyCollection<T> jobResult = null;

            using (cancelTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(config?.Timeout ?? JobTimeout)))
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                cancelTokenSource.Token.Register(() =>
                {
                    taskCompletionSource.TrySetCanceled();
                });

                Task<IReadOnlyCollection<T>> downloadTask = InternalDownload(config as CameraConfig, from, to);
                Task completedTask = await Task.WhenAny(downloadTask, taskCompletionSource.Task);

                if (completedTask == downloadTask)
                {
                    jobResult = await downloadTask;
                    taskCompletionSource.TrySetResult(true);
                }

                await taskCompletionSource.Task;
            }

            cancelTokenSource = null;
            return jobResult;
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

        private async Task<IReadOnlyCollection<T>> InternalDownload(CameraConfig config, DateTime from, DateTime to)
        {
            DateTime appStart = DateTime.Now;

            logger.Info($"{config.Alias} - Internal download...");

            var result = await ProcessCameraAsync(config, from, to);
            if (result.Count > 0)
            {
                PrintStatistic(config?.DestinationFolder);
                var duration = (DateTime.Now - appStart).TotalSeconds;
                logger.Info($"{config.Alias} - Internal download. Done. Duration  : {duration.FormatSeconds()}");
            }
            else
            {
                logger.Warn($"{config.Alias}, {from} - {to} : No files downloaded");
            }

            return result;
        }

        private async Task<IReadOnlyCollection<T>> ProcessCameraAsync(CameraConfig config, DateTime periodStart, DateTime periodEnd)
        {
            var result = new List<T>();
            using (Client = clientFactory.Create(config))
            {
                Client.InitializeClient();
                ThrowIfCancellationRequested();

                if (Client.Login())
                {
                    if (config.SyncTime && Client is HikBaseClient)
                    {
                        ((HikBaseClient)Client).SyncTime();
                    }

                    ThrowIfCancellationRequested();
                    logger.Info($"{config.Alias} - Reading remote files...");
                    var remoteFiles = await GetRemoteFilesList(periodStart, periodEnd);
                    logger.Info($"{config.Alias} - Reading remote files. Done");

                    logger.Info($"{config.Alias} - Downloading files...");
                    var downloadedFiles = await DownloadFilesFromClientAsync(remoteFiles, cancelTokenSource?.Token ?? CancellationToken.None);
                    result.AddRange(downloadedFiles);
                    logger.Info($"{config.Alias} - Downloading files. Done");
                }
                else
                {
                    logger.Warn($"{config.Alias} - Unable to login");
                }
            }

            return result;
        }

        private void PrintStatistic(string destinationFolder)
        {
            StringBuilder statisticsSb = new StringBuilder();
            statisticsSb.AppendLine();
            statisticsSb.AppendLine($"{"Directory Size",-24}: {directoryHelper.DirSize(destinationFolder).FormatBytes()}");
            statisticsSb.AppendLine($"{"Free space",-24}: {directoryHelper.GetTotalFreeSpaceGb(destinationFolder)}");
            statisticsSb.AppendLine(new string('_', 40)); // separator

            logger.Info(statisticsSb.ToString());
        }
    }
}

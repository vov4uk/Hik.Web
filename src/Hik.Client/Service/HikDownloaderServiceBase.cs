using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;

namespace Hik.Client.Service
{
    public abstract class HikDownloaderServiceBase : RecurrentJobBase
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

        public abstract Task<IReadOnlyCollection<MediaFileDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDTO> remoteFiles, CancellationToken token);

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            CameraConfig cameraConfig = config as CameraConfig ?? throw new ArgumentNullException(nameof(config));
            IReadOnlyCollection<MediaFileDTO> jobResult = null;

            using (cancelTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(cameraConfig.Timeout)))
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                cancelTokenSource.Token.Register(() =>
                {
                    taskCompletionSource.TrySetCanceled();
                });

                Task<IReadOnlyCollection<MediaFileDTO>> downloadTask = InternalDownload(cameraConfig, from, to);
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

        private async Task<IReadOnlyCollection<MediaFileDTO>> InternalDownload(CameraConfig config, DateTime from, DateTime to)
        {
            var result = await ProcessCameraAsync(config, from, to);
            if (!result.Any())
            {
                logger.Warn($"{config.Alias}, {from} - {to} : No files downloaded");
            }

            return result;
        }

        private async Task<IReadOnlyCollection<MediaFileDTO>> ProcessCameraAsync(CameraConfig config, DateTime periodStart, DateTime periodEnd)
        {
            var result = new List<MediaFileDTO>();
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

                    var remoteFiles = await GetRemoteFilesList(periodStart, periodEnd);
                    var downloadedFiles = await DownloadFilesFromClientAsync(remoteFiles, cancelTokenSource?.Token ?? CancellationToken.None);
                    result.AddRange(downloadedFiles);
                }
                else
                {
                    logger.Warn($"{config.Alias} - Unable to login");
                }
            }

            return result;
        }
    }
}

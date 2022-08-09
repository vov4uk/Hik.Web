using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hik.Api;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;

namespace Hik.Client.Service
{
    public abstract class HikDownloaderServiceBase : RecurrentJobBase
    {
        protected const int JobTimeout = 30;
        private readonly IClientFactory clientFactory;
        private CancellationTokenSource cancelTokenSource;

        protected HikDownloaderServiceBase(
            IDirectoryHelper directoryHelper,
            IClientFactory clientFactory,
            ILogger logger)
            : base(directoryHelper, logger)
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
                logger.LogWarning("Cancel signal was sent");
            }
            else
            {
                logger.LogWarning("Nothing to Cancel");
            }
        }

        public abstract Task<IReadOnlyCollection<MediaFileDto>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd);

        public abstract Task<IReadOnlyCollection<MediaFileDto>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDto> remoteFiles, CancellationToken token);

        protected override async Task<IReadOnlyCollection<MediaFileDto>> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            try
            {
                CameraConfig cameraConfig = config as CameraConfig ?? throw new ArgumentNullException(nameof(config));
                IReadOnlyCollection<MediaFileDto> jobResult = null;

                using (cancelTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(cameraConfig.Timeout)))
                {
                    TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

                    cancelTokenSource.Token.Register(() =>
                    {
                        taskCompletionSource.TrySetCanceled();
                    });

                    Task<IReadOnlyCollection<MediaFileDto>> downloadTask = InternalDownload(cameraConfig, from, to);
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
            catch (HikException ex)
            {
                throw new InvalidOperationException($"{ex.ErrorMessage}, Code : {ex.ErrorCode}");
            }
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
                throw new OperationCanceledException("Operation canceled");
            }
        }

        private async Task<IReadOnlyCollection<MediaFileDto>> InternalDownload(CameraConfig config, DateTime from, DateTime to)
        {
            var result = await ProcessCameraAsync(config, from, to);
            if (!result.Any())
            {
                logger.LogWarning($"{from} - {to} : No files downloaded");
            }

            return result;
        }

        private async Task<IReadOnlyCollection<MediaFileDto>> ProcessCameraAsync(CameraConfig config, DateTime periodStart, DateTime periodEnd)
        {
            var result = new List<MediaFileDto>();
            using (Client = clientFactory.Create(config))
            {
                Client.InitializeClient();
                ThrowIfCancellationRequested();

                if (Client.Login())
                {
                    if (config.SyncTime && Client is HikBaseClient hikClient)
                    {
                        hikClient.SyncTime();
                    }

                    ThrowIfCancellationRequested();

                    var remoteFiles = await GetRemoteFilesList(periodStart, periodEnd);
                    var downloadedFiles = await DownloadFilesFromClientAsync(remoteFiles, cancelTokenSource?.Token ?? CancellationToken.None);
                    result.AddRange(downloadedFiles);
                }
            }

            return result;
        }
    }
}

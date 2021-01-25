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

namespace Hik.Client.Service
{
    public abstract class HikDownloaderServiceBase<T> : RecurrentJobBase<T>
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

        public event EventHandler<FileDownloadedEventArgs> FileDownloaded;

        protected IMapper Mapper { get; private set; }

        protected IClient Client { get; private set; }

        public override async Task<IReadOnlyCollection<T>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to)
        {
            IReadOnlyCollection<T> jobResult = null;
            try
            {
                using (cancelTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(JobTimeout)))
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
            }
            catch (Exception ex)
            {
                HandleException(ex, config);
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
                logger.Warn("Cancel signal was sent");
            }
            else
            {
                logger.Warn("Nothing to Cancel");
            }
        }

        public abstract Task<IReadOnlyCollection<MediaFileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd);

        public abstract Task<IReadOnlyCollection<T>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDTO> remoteFiles, CancellationToken token);

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

            logger.Info($"{config.Alias} - Internal download...");

            var result = await ProcessCameraAsync(config, from, to);
            if (result.Count > 0)
            {
                PrintStatistic(config?.DestinationFolder);
                var duration = (DateTime.Now - appStart).TotalSeconds;
                logger.Info($"{config.Alias} -Internal download. Done. Duration  : {duration.FormatSeconds()}");
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
            statisticsSb.AppendLine();
            statisticsSb.AppendLine($"{"Directory Size",-24}: {Utils.FormatBytes(directoryHelper.DirSize(destinationFolder))}");
            statisticsSb.AppendLine($"{"Free space",-24}: {Utils.FormatBytes(directoryHelper.GetTotalFreeSpace(destinationFolder))}");
            statisticsSb.AppendLine(new string('_', 40)); // separator

            logger.Info(statisticsSb.ToString());
        }

        private void HandleException(Exception ex, BaseConfig config)
        {
            OnExceptionFired(new ExceptionEventArgs(ex), config);

            ForceExit();
        }
    }
}

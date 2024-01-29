using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Dahua.Api.Abstractions;
using Dahua.Api.Data;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;

namespace Hik.Client.Client
{
    public class DahuaVideoClient : IDownloaderClient
    {
        private const int ProgressCheckPeriodMilliseconds = 5000;
        private readonly CameraConfig config;
        private readonly IDirectoryHelper dirHelper;
        private readonly IFilesHelper filesHelper;
        private readonly ILogger logger;
        private readonly IDahuaSDK dahuaSDK;
        private IDahuaApi dahuaApi;
        private bool disposedValue = false;
        private long downloadId = -1;

        public DahuaVideoClient(
            CameraConfig config,
            IDahuaSDK dahuaSDK,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper,
            ILogger logger)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.dahuaSDK = dahuaSDK;
            this.filesHelper = filesHelper;
            this.dirHelper = directoryHelper;
            this.Mapper = mapper;
            this.logger = logger;
        }

        protected IMapper Mapper { get; private set; }

        private bool IsDownloading => downloadId >= 0;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public async Task<bool> DownloadFileAsync(MediaFileDto remoteFile, CancellationToken token)
        {
            string targetFilePath = GetPathSafety(remoteFile);
            string tempFile = filesHelper.GetTempFileName() + ".mp4";
            if (this.StartVideoDownload(remoteFile, targetFilePath, tempFile))
            {
                do
                {
                    await Task.Delay(ProgressCheckPeriodMilliseconds, token);
                    token.ThrowIfCancellationRequested();
                    this.UpdateVideoProgress();
                }
                while (this.IsDownloading);

                filesHelper.RenameFile(tempFile, targetFilePath);
                remoteFile.Size = filesHelper.FileSize(targetFilePath);
                remoteFile.Path = targetFilePath;

                return true;
            }

            return false;
        }

        public void ForceExit()
        {
            logger.Warning("Force Exit");
            StopDownload();
            Dispose(true);
        }

        public Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);
            logger.Information($"Get videos from {periodStart} to {periodEnd}");

            IReadOnlyCollection<IRemoteFile> remoteFiles = dahuaApi.VideoService.FindFiles(periodStart, periodEnd);
            return Task.FromResult(Mapper.Map<IReadOnlyCollection<MediaFileDto>>(remoteFiles));
        }

        public void InitializeClient()
        {
            dahuaSDK.Initialize();
        }

        public bool Login()
        {
            if (dahuaApi == null)
            {
                dahuaApi = dahuaSDK.Login(config.Camera.IpAddress, config.Camera.PortNumber, config.Camera.UserName, config.Camera.Password);
                logger.Information("Successfully logged to {IpAddress}", config.Camera.IpAddress);
                return true;
            }
            else
            {
                logger.Warning("Already logged in");
                return false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                logger.Information("Logout the device");
                dahuaApi?.Logout();

                dahuaApi = null;

                dahuaSDK.Cleanup();
                disposedValue = true;
            }
        }

        private static string ToFileNameString(MediaFileDto file)
        {
            return file.ToVideoFileNameString();
        }

        private static void ValidateDateParameters(DateTime start, DateTime end)
        {
            if (end <= start)
            {
                throw new ArgumentException("Start period grater than end");
            }
        }

        private bool StartVideoDownload(MediaFileDto file, string targetFilePath, string tempFile)
        {
            if (!IsDownloading)
            {
                if (!filesHelper.FileExists(targetFilePath))
                {
                    downloadId = dahuaApi.VideoService.StartDownloadFile(new RemoteFile(file.Name, file.Date, file.Duration.Value, (uint)file.Size), tempFile);

                    logger.Information($"{file.ToVideoUserFriendlyString()} - downloading");
                    return true;
                }

                logger.Warning($"{file.ToVideoUserFriendlyString()} - exist");
            }
            else
            {
                logger.Warning("Downloading, please stop firstly!");
            }

            return false;
        }

        private void StopDownload()
        {
            if (IsDownloading)
            {
                dahuaApi.VideoService.StopDownloadFile(downloadId);
                ResetDownloadStatus();
            }
            else
            {
                logger.Warning("File not downloading now");
            }
        }

        private void ResetDownloadStatus()
        {
            downloadId = -1;
        }

        private string GetPathSafety(MediaFileDto remoteFile)
        {
            string workingDirectory = GetWorkingDirectory(remoteFile);
            dirHelper.CreateDirIfNotExist(workingDirectory);

            return filesHelper.CombinePath(workingDirectory, ToFileNameString(remoteFile));
        }

        private string GetWorkingDirectory(MediaFileDto file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, ToDirectoryNameString(file));
        }

        private string ToDirectoryNameString(MediaFileDto file)
        {
            return config.SaveFilesToRootFolder ? string.Empty : file.Date.ToDirectoryName();
        }

        private void UpdateVideoProgress()
        {
            if (IsDownloading)
            {
                var downloadProgress = dahuaApi.VideoService.GetDownloadPosition(downloadId);

                if (!downloadProgress.success)
                {
                    StopDownload();
                    logger.Debug("Downloaded");
                }
            }
            else
            {
                logger.Warning("File not downloading now");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;

namespace Hik.Client
{
    public abstract class HikBaseClient : IDownloaderClient
    {
        protected const int ProgressBarMaximum = 100;
        protected const int ProgressBarMinimum = 0;

        protected readonly CameraConfig config;
        protected readonly IDirectoryHelper dirHelper;
        protected readonly IFilesHelper filesHelper;
        protected readonly ILogger logger;
        protected IHikApi hikApi;
        protected IHikSDK hikSDK;
        protected int downloadId = -1;
        private bool disposedValue = false;

        protected HikBaseClient(
            CameraConfig config,
            IHikSDK hikSDK,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper,
            ILogger logger)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.hikSDK = hikSDK;
            this.filesHelper = filesHelper;
            this.dirHelper = directoryHelper;
            this.Mapper = mapper;
            this.logger = logger;
        }

        protected IMapper Mapper { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void ForceExit()
        {
            logger.Warning("Force Exit");
            StopDownload();
            Dispose(true);
        }

        public abstract Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd);

        public void InitializeClient()
        {
            string sdkLogsPath = filesHelper.CombinePath(Environment.CurrentDirectory, "logs", config.Camera.IpAddress + "_SdkLog");
            dirHelper.CreateDirIfNotExist(sdkLogsPath);
            dirHelper.CreateDirIfNotExist(config.DestinationFolder);

            logger.Information("SDK Logs : {sdkLogsPath}", sdkLogsPath);
            hikSDK.Initialize(logDirectory: sdkLogsPath);
        }

        public bool Login()
        {
            if (hikApi == null)
            {
                hikApi = hikSDK.Login(config.Camera.IpAddress, config.Camera.PortNumber, config.Camera.UserName, config.Camera.Password);
                logger.Information("Successfully logged to {IpAddress}", config.Camera.IpAddress);
                var status = hikApi.ConfigService.GetHddStatus();

                logger.Information(status?.ToString());

                if (status is { IsErrorStatus: true })
                {
                    throw new InvalidOperationException("HD error");
                }

                return true;
            }
            else
            {
                logger.Warning("Already logged in");
                return false;
            }
        }

        public void SyncTime()
        {
            var cameraTime = hikApi.ConfigService.GetTime();
            var currentTime = DateTime.Now;
            if (Math.Abs((currentTime - cameraTime).TotalSeconds) > config.SyncTimeDeltaSeconds)
            {
                hikApi.ConfigService.SetTime(currentTime);
                logger.Warning("Camera time updated :from {cameraTime} to {currentTime}", cameraTime, currentTime);
            }
        }

        public abstract Task<bool> DownloadFileAsync(MediaFileDto remoteFile, CancellationToken token);

        protected static void ValidateDateParameters(DateTime start, DateTime end)
        {
            if (end <= start)
            {
                throw new ArgumentException("Start period grater than end");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                logger.Information("Logout the device");
                hikApi?.Logout();

                hikApi = null;

                hikSDK.Cleanup();
                disposedValue = true;
            }
        }

        protected string GetPathSafety(MediaFileDto remoteFile)
        {
            string workingDirectory = GetWorkingDirectory(remoteFile);
            dirHelper.CreateDirIfNotExist(workingDirectory);

            return filesHelper.CombinePath(workingDirectory, ToFileNameString(remoteFile));
        }

        protected void ResetDownloadStatus()
        {
            downloadId = -1;
        }

        protected abstract void StopDownload();

        protected abstract string ToDirectoryNameString(MediaFileDto file);

        protected abstract string ToFileNameString(MediaFileDto file);

        private string GetWorkingDirectory(MediaFileDto file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, ToDirectoryNameString(file));
        }
    }
}

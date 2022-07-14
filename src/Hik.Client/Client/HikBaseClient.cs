using System;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Api.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;

namespace Hik.Client
{
    public abstract class HikBaseClient : IDisposable
    {
        protected const int ProgressBarMaximum = 100;
        protected const int ProgressBarMinimum = 0;
        protected readonly CameraConfig config;
        protected readonly IHikApi hikApi;
        protected readonly IFilesHelper filesHelper;
        protected readonly IDirectoryHelper dirHelper;
        protected readonly ILogger logger;
        protected int downloadId = -1;
        protected Session session;
        private bool disposedValue = false;

        protected HikBaseClient(
            CameraConfig config,
            IHikApi hikApi,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper,
            ILogger logger)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.dirHelper = directoryHelper;
            this.Mapper = mapper;
            this.logger = logger;
        }

        protected IMapper Mapper { get; private set; }

        public void InitializeClient()
        {
            string sdkLogsPath = filesHelper.CombinePath(Environment.CurrentDirectory, "logs", config.Alias + "_SdkLog");
            dirHelper.CreateDirIfNotExist(sdkLogsPath);
            dirHelper.CreateDirIfNotExist(config.DestinationFolder);

            logger.LogInformation($"SDK Logs : {sdkLogsPath}");
            hikApi.Initialize();
            hikApi.SetupLogs(3, sdkLogsPath, false);
            hikApi.SetConnectTime(2000, 1);
            hikApi.SetReconnect(10000, 1);
        }

        public bool Login()
        {
            if (session == null)
            {
                session = hikApi.Login(config.IpAddress, config.PortNumber, config.UserName, config.Password);
                logger.LogInformation($"Sucessfull login to {config}");
                var status = hikApi.GetHddStatus(session.UserId);

                logger.LogInformation(status?.ToString());

                if (status is { IsErrorStatus: true })
                {
                    throw new InvalidOperationException("HD error");
                }

                return true;
            }
            else
            {
                logger.LogWarning("HikBaseClient.Login : Already logged in");
                return false;
            }
        }

        public void SyncTime()
        {
            if (config.SyncTime)
            {
                var cameraTime = hikApi.GetTime(session.UserId);
                logger.LogInformation($"Camera time :{cameraTime}");
                var currentTime = DateTime.Now;
                if (Math.Abs((currentTime - cameraTime).TotalSeconds) > config.SyncTimeDeltaSeconds)
                {
                    hikApi.SetTime(currentTime, session.UserId);
                    logger.LogWarning($"Camera time updated :{currentTime}");
                }
            }
        }

        public void ForceExit()
        {
            logger.LogWarning("HikBaseClient.ForceExit");
            StopDownload();
            Dispose(true);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                logger.LogInformation("Logout the device");
                if (session != null)
                {
                    hikApi.Logout(session.UserId);
                }

                session = null;

                hikApi.Cleanup();
                disposedValue = true;
            }
        }

        protected abstract string ToFileNameString(MediaFileDto file);

        protected abstract string ToDirectoryNameString(MediaFileDto file);

        protected abstract void StopDownload();

        protected void ValidateDateParameters(DateTime start, DateTime end)
        {
            if (end <= start)
            {
                throw new ArgumentException("Start period grater than end");
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

        private string GetWorkingDirectory(MediaFileDto file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, ToDirectoryNameString(file));
        }
    }
}

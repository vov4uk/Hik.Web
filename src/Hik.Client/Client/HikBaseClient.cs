using System;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Api.Data;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using NLog;

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
        protected ILogger logger = LogManager.GetCurrentClassLogger();
        protected int downloadId = -1;
        protected Session session;
        private bool disposedValue = false;

        protected HikBaseClient(
            CameraConfig config,
            IHikApi hikApi,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config;
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.dirHelper = directoryHelper;
            this.Mapper = mapper;
        }

        protected IMapper Mapper { get; private set; }

        public void InitializeClient()
        {
            string sdkLogsPath = filesHelper.CombinePath(DirectoryHelper.AssemblyDirectory, "logs", config.Alias + "_SdkLog");
            dirHelper.CreateDirIfNotExist(sdkLogsPath);
            dirHelper.CreateDirIfNotExist(config.DestinationFolder);

            logger.Info($"SDK Logs : {sdkLogsPath}");
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
                logger.Info($"Sucessfull login to {config}");
                var status = hikApi.GetHddStatus(session.UserId);

                logger.Info(status?.ToString());

                if (status != null && status.IsErrorStatus)
                {
                    throw new InvalidOperationException("HD error");
                }

                return true;
            }
            else
            {
                logger.Warn("HikBaseClient.Login : Already logged in");
                return false;
            }
        }

        public void ForceExit()
        {
            logger.Warn("HikBaseClient.ForceExit");
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
                logger.Info($"Logout the device");
                if (session != null)
                {
                    hikApi.Logout(session.UserId);
                }

                session = null;

                hikApi.Cleanup();
                disposedValue = true;
            }
        }

        protected abstract string ToFileNameString(MediaFileDTO file);

        protected abstract string ToDirectoryNameString(MediaFileDTO file);

        protected abstract void StopDownload();

        protected void ValidateDateParameters(DateTime start, DateTime end)
        {
            if (end <= start)
            {
                throw new ArgumentException("Start period grater than end");
            }
        }

        protected string GetPathSafety(MediaFileDTO remoteFile)
        {
            string workingDirectory = GetWorkingDirectory(remoteFile);
            dirHelper.CreateDirIfNotExist(workingDirectory);

            return filesHelper.CombinePath(workingDirectory, ToFileNameString(remoteFile));
        }

        protected void ResetDownloadStatus()
        {
            downloadId = -1;
        }

        private string GetWorkingDirectory(MediaFileDTO file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, ToDirectoryNameString(file));
        }
    }
}

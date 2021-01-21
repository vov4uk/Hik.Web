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
    public abstract class HikBaseClient
    {
        protected const int ProgressBarMaximum = 100;
        protected const int ProgressBarMinimum = 0;
        protected readonly CameraConfig config;
        protected readonly IHikApi hikApi;
        protected readonly IFilesHelper filesHelper;
        protected ILogger logger = LogManager.GetCurrentClassLogger();
        protected int downloadId = -1;
        protected MediaFileDTO currentDownloadFile;
        protected Session session;
        private bool disposedValue = false;

        public HikBaseClient(
            CameraConfig config,
            IHikApi hikApi,
            IFilesHelper filesHelper,
            IMapper mapper)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config;
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.Mapper = mapper;
        }

        public bool IsDownloading => downloadId >= 0;

        protected IMapper Mapper { get; private set; }

        public void InitializeClient()
        {
            string sdkLogsPath = filesHelper.CombinePath(DirectoryHelper.AssemblyDirectory, "logs", config.Alias + "_SdkLog");
            filesHelper.FolderCreateIfNotExist(sdkLogsPath);
            filesHelper.FolderCreateIfNotExist(config.DestinationFolder);

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
                var status = CheckHardDriveStatus();

                logger.Info(status?.ToString());

                if (status != null && status.IsErrorStatus)
                {
                    throw new InvalidOperationException("HD error");
                }

                return true;
            }
            else
            {
                logger.Warn("HikClient.Login : Already logged in");
                return false;
            }
        }

        public void ForceExit()
        {
            logger.Warn("HikClient.ForceExit");
            StopDownload();
            DeleteCurrentFile();
        }

        public HdInfo CheckHardDriveStatus()
        {
            return hikApi.GetHddStatus(session.UserId);
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
                currentDownloadFile = null;

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
            filesHelper.FolderCreateIfNotExist(workingDirectory);

            string destinationFilePath = GetFullPath(remoteFile, workingDirectory);
            return destinationFilePath;
        }

        protected void ResetDownloadStatus()
        {
            downloadId = -1;
        }

        private string GetWorkingDirectory(MediaFileDTO file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, ToDirectoryNameString(file));
        }

        private string GetFullPath(MediaFileDTO file, string directory = null)
        {
            string folder = directory ?? GetWorkingDirectory(file);
            return filesHelper.CombinePath(folder, ToFileNameString(file));
        }

        private void DeleteCurrentFile()
        {
            if (currentDownloadFile != null)
            {
                string path = GetFullPath(currentDownloadFile);
                logger.Warn($"Removing file {path}");
                filesHelper.DeleteFile(path);

                currentDownloadFile = null;
            }
            else
            {
                logger.Warn("HikClient.DeleteCurrentFile : Nothing to delete");
            }
        }
    }
}

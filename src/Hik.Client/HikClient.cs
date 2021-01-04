namespace Hik.Client
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;
    using Hik.Api.Abstraction;
    using Hik.Api.Data;
    using Hik.Client.Abstraction;
    using Hik.Client.Helpers;
    using Hik.DTO.Config;
    using NLog;

    public class HikClient : IHikClient
    {
        private const int ProgressBarMaximum = 100;
        private const int ProgressBarMinimum = 0;
        private readonly CameraConfig config;
        private readonly IHikApi hikApi;
        private readonly IFilesHelper filesHelper;
        private ILogger logger = LogManager.GetCurrentClassLogger();
        private int downloadId = -1;
        private IRemoteFile currentDownloadFile;
        private Session session;
        private bool disposedValue = false;

        public HikClient(CameraConfig config, IHikApi hikApi, IFilesHelper filesHelper, ILogger logger)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config;
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.logger = logger;
        }

        public bool IsDownloading => downloadId >= 0;

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
                return true;
            }
            else
            {
                logger.Warn("HikClient.Login : Already logged in");
                return false;
            }
        }

        public bool StartVideoDownload(IRemoteFile remoteFile)
        {
            if (!IsDownloading)
            {
                string destinationFilePath = GetPathSafety(remoteFile);

                if (!CheckLocalVideoExist(destinationFilePath, remoteFile.Size))
                {
                    downloadId = hikApi.VideoService.StartDownloadFile(session.UserId, remoteFile.Name, destinationFilePath);

                    logger.Info($"{remoteFile.ToUserFriendlyString()}- downloading");

                    currentDownloadFile = remoteFile;
                    return true;
                }

                logger.Info($"{remoteFile.ToUserFriendlyString()}- exist");
                return false;
            }
            else
            {
                logger.Warn("HikClient.StartDownload : Downloading, please stop firstly!");
                return false;
            }
        }

        public bool PhotoDownload(RemotePhotoFile remoteFile)
        {
            if (!IsDownloading)
            {
                string destinationFilePath = GetPathSafety(remoteFile);

                if (!CheckLocalPhotoExist(destinationFilePath, remoteFile.Size))
                {
                    string tempFile = remoteFile.ToFileNameString();
                    hikApi.PhotoService.DownloadFile(session.UserId, remoteFile, tempFile);

                    SetDate(tempFile, destinationFilePath, remoteFile.Date);
                    filesHelper.DeleteFile(tempFile);

                    return true;
                }

                return false;
            }
            else
            {
                logger.Warn("HikClient.PhotoDownload : Downloading, please stop firstly!");
                return false;
            }
        }

        public void StopVideoDownload()
        {
            if (IsDownloading)
            {
                hikApi.VideoService.StopDownloadFile(downloadId);
                ResetDownloadStatus();
            }
            else
            {
                logger.Warn("HikClient.StopDownload : File not downloading now");
            }
        }

        public void UpdateVideoProgress()
        {
            if (IsDownloading)
            {
                int downloadProgress = hikApi.VideoService.GetDownloadPosition(downloadId);

                UpdateProgressInternal(downloadProgress);
            }
            else
            {
                logger.Warn("HikClient.UpdateProgress : File not downloading now");
            }
        }

        public void ForceExit()
        {
            logger.Warn("HikClient.ForceExit");
            StopVideoDownload();
            DeleteCurrentFile();
        }

        public HdInfo CheckHardDriveStatus()
        {
            return hikApi.GetHddStatus(session.UserId);
        }

        public Task<IList<RemoteVideoFile>> FindVideosAsync(DateTime periodStart, DateTime periodEnd)
        {
            Guard.IsValid(
                () => periodStart,
                periodStart,
                start => start < periodEnd,
                "Start period grater than end");

            logger.Info($"Get videos from {periodStart} to {periodEnd}");

            return hikApi.VideoService.FindFilesAsync(periodStart, periodEnd, session);
        }

        public Task<IList<RemotePhotoFile>> FindPhotosAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            logger.Info($"Get photos from {periodStart} to {periodEnd}");

            return hikApi.PhotoService.FindFilesAsync(periodStart, periodEnd, session);
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

        private string GetWorkingDirectory(IRemoteFile file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, file.ToDirectoryNameString());
        }

        private string GetFullPath(IRemoteFile file, string directory = null)
        {
            string folder = directory ?? GetWorkingDirectory(file);
            return filesHelper.CombinePath(folder, file.ToFileNameString());
        }

        private void ResetDownloadStatus()
        {
            downloadId = -1;
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

        private void ValidateDateParameters(DateTime start, DateTime end)
        {
            if (end <= start)
            {
                throw new ArgumentException("Start period grater than end");
            }
        }

        private void UpdateProgressInternal(int progressValue)
        {
            if (progressValue == ProgressBarMaximum)
            {
                StopVideoDownload();
                currentDownloadFile = null;

                logger.Info("- downloaded");
            }
            else if (progressValue < ProgressBarMinimum || progressValue > ProgressBarMaximum)
            {
                StopVideoDownload();
                throw new InvalidOperationException($"HikClient.UpdateDownloadProgress failed, progress value = {progressValue}");
            }
        }

        private string GetPathSafety(IRemoteFile remoteFile)
        {
            string workingDirectory = GetWorkingDirectory(remoteFile);
            filesHelper.FolderCreateIfNotExist(workingDirectory);

            string destinationFilePath = GetFullPath(remoteFile, workingDirectory);
            return destinationFilePath;
        }

        private void SetDate(string path, string newPath, DateTime date)
        {
            using (Image image = Image.FromFile(path))
            {
                var newItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
                newItem.Value = System.Text.Encoding.ASCII.GetBytes(date.ToString("yyyy':'MM':'dd' 'HH':'mm':'ss"));
                newItem.Type = 2;
                newItem.Id = 306;
                image.SetPropertyItem(newItem);
                image.Save(newPath, image.RawFormat);
            }
        }

        private bool CheckLocalVideoExist(string path, long size)
        {
            // Downloaded video file is 40 bytes bigger than remote file
            // This const was taken on debug
            return filesHelper.FileExists(path, size + 40);
        }

        private bool CheckLocalPhotoExist(string path, long size)
        {
            // Downloaded video file is bigger than remote file
            // 56 bytes for 2MP camera
            // 70 bytes for 4MP camera
            // This const was taken on runtime
            long fileSize = filesHelper.FileSize(path);
            return size + 70 == fileSize || size + 56 == fileSize;
        }
    }
}
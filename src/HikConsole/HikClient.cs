using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using HikApi.Abstraction;
using HikApi.Data;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Helpers;

namespace HikConsole
{
    public class HikClient : IHikClient
    {
        private const int ProgressBarMaximum = 100;
        private const int ProgressBarMinimum = 0;
        private readonly CameraConfig config;
        private readonly IHikApi hikApi;
        private readonly IFilesHelper filesHelper;
        private readonly IProgressBarFactory progressFactory;
        private readonly ILogger logger;
        private int downloadId = -1;
        private IRemoteFile currentDownloadFile;
        private Session session;
        private IProgressBar progress;
        private bool disposedValue = false;

        public HikClient(CameraConfig config, IHikApi hikApi, IFilesHelper filesHelper, IProgressBarFactory progressFactory, ILogger logger)
        {
            this.config = config;
            this.hikApi = hikApi;
            this.filesHelper = filesHelper;
            this.progressFactory = progressFactory;
            this.logger = logger;
        }

        public bool IsDownloading => this.downloadId >= 0;

        public void InitializeClient()
        {
            string sdkLogsPath = this.filesHelper.CombinePath(DirectoryHelper.AssemblyDirectory, "logs", this.config.Alias + "_SdkLog");
            this.filesHelper.FolderCreateIfNotExist(sdkLogsPath);
            this.filesHelper.FolderCreateIfNotExist(this.config.DestinationFolder);

            this.logger.Info($"SDK Logs : {sdkLogsPath}");
            this.hikApi.Initialize();
            this.hikApi.SetupLogs(3, sdkLogsPath, false);
            this.hikApi.SetConnectTime(2000, 1);
            this.hikApi.SetReconnect(10000, 1);
        }

        public bool Login()
        {
            if (this.session == null)
            {
                this.session = this.hikApi.Login(this.config.IpAddress, this.config.PortNumber, this.config.UserName, this.config.Password);
                this.logger.Info($"Sucessfull login to {this.config}");
                return true;
            }
            else
            {
                this.logger.Warn("HikClient.Login : Already logged in");
                return false;
            }
        }

        public bool StartVideoDownload(RemoteVideoFile remoteFile)
        {
            if (!this.IsDownloading)
            {
                string destinationFilePath = this.GetPathSafety(remoteFile);

                if (!this.CheckLocalVideoExist(destinationFilePath, remoteFile.Size))
                {
                    this.downloadId = this.hikApi.VideoService.StartDownloadFile(this.session.UserId, remoteFile.Name, destinationFilePath);

                    this.logger.Info($"{remoteFile.ToUserFriendlyString()}- downloading");

                    this.currentDownloadFile = remoteFile;
                    this.progress = this.config.ShowProgress ? this.progressFactory.Create() : default(IProgressBar);
                    return true;
                }

                this.logger.Info($"{remoteFile.ToUserFriendlyString()}- exist");
                return false;
            }
            else
            {
                this.logger.Warn("HikClient.StartDownload : Downloading, please stop firstly!");
                return false;
            }
        }

        public bool PhotoDownload(RemotePhotoFile remoteFile)
        {
            if (!this.IsDownloading)
            {
                string destinationFilePath = this.GetPathSafety(remoteFile);

                if (!this.CheckLocalPhotoExist(destinationFilePath, remoteFile.Size))
                {
                    string tempFile = remoteFile.ToFileNameString();
                    this.hikApi.PhotoService.DownloadFile(this.session.UserId, remoteFile, tempFile);

                    this.SetDate(tempFile, destinationFilePath, remoteFile.Date);
                    this.filesHelper.DeleteFile(tempFile);

                    return true;
                }

                return false;
            }
            else
            {
                this.logger.Warn("HikClient.PhotoDownload : Downloading, please stop firstly!");
                return false;
            }
        }

        public void StopVideoDownload()
        {
            if (this.IsDownloading)
            {
                this.hikApi.VideoService.StopDownloadFile(this.downloadId);
                this.ResetDownloadStatus();
            }
            else
            {
                this.logger.Warn("HikClient.StopDownload : File not downloading now");
            }
        }

        public void UpdateVideoProgress()
        {
            if (this.IsDownloading)
            {
                int downloadProgress = this.hikApi.VideoService.GetDownloadPosition(this.downloadId);

                this.UpdateProgressInternal(downloadProgress);
            }
            else
            {
                this.logger.Warn("HikClient.UpdateProgress : File not downloading now");
            }
        }

        public void ForceExit()
        {
            this.logger.Warn("HikClient.ForceExit");
            this.StopVideoDownload();
            this.DeleteCurrentFile();
        }

        public HdInfo CheckHardDriveStatus()
        {
            return this.hikApi.GetHddStatus(this.session.UserId);
        }

        public Task<IList<RemoteVideoFile>> FindVideosAsync(DateTime periodStart, DateTime periodEnd)
        {
            Guard.IsValid(
                () => periodStart,
                periodStart,
                start => start < periodEnd,
                "Start period grater than end");

            this.logger.Info($"Get videos from {periodStart} to {periodEnd}");

            return this.hikApi.VideoService.FindFilesAsync(periodStart, periodEnd, this.session);
        }

        public Task<IList<RemotePhotoFile>> FindPhotosAsync(DateTime periodStart, DateTime periodEnd)
        {
            this.ValidateDateParameters(periodStart, periodEnd);

            this.logger.Info($"Get photos from {periodStart} to {periodEnd}");

            return this.hikApi.PhotoService.FindFilesAsync(periodStart, periodEnd, this.session);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this.progress?.Dispose();
                }

                this.logger.Info($"Logout the device");
                if (this.session != null)
                {
                    this.hikApi.Logout(this.session.UserId);
                }

                this.session = null;
                this.progress = null;
                this.currentDownloadFile = null;

                this.hikApi.Cleanup();
                this.disposedValue = true;
            }
        }

        private string GetWorkingDirectory(IRemoteFile file)
        {
            return this.filesHelper.CombinePath(this.config.DestinationFolder, file.ToDirectoryNameString());
        }

        private string GetFullPath(IRemoteFile file, string directory = null)
        {
            string folder = directory ?? this.GetWorkingDirectory(file);
            return this.filesHelper.CombinePath(folder, file.ToFileNameString());
        }

        private void ResetDownloadStatus()
        {
            this.downloadId = -1;
            this.progress?.Dispose();
            this.progress = null;
        }

        private void DeleteCurrentFile()
        {
            if (this.currentDownloadFile != null)
            {
                string path = this.GetFullPath(this.currentDownloadFile);
                this.logger.Warn($"Removing file {path}");
                this.filesHelper.DeleteFile(path);

                this.currentDownloadFile = null;
            }
            else
            {
                this.logger.Warn("HikClient.DeleteCurrentFile : Nothing to delete");
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
            if (progressValue >= ProgressBarMinimum && progressValue < ProgressBarMaximum)
            {
                this.progress?.Report((double)progressValue / ProgressBarMaximum);
            }
            else if (progressValue == ProgressBarMaximum)
            {
                this.StopVideoDownload();
                this.currentDownloadFile = null;

                this.logger.Info("- downloaded");
            }
            else
            {
                this.StopVideoDownload();
                throw new InvalidOperationException($"HikClient.UpdateDownloadProgress failed, progress value = {progressValue}");
            }
        }

        private string GetPathSafety(IRemoteFile remoteFile)
        {
            string workingDirectory = this.GetWorkingDirectory(remoteFile);
            this.filesHelper.FolderCreateIfNotExist(workingDirectory);

            string destinationFilePath = this.GetFullPath(remoteFile, workingDirectory);
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
            return this.filesHelper.FileExists(path, size + 40);
        }

        private bool CheckLocalPhotoExist(string path, long size)
        {
            // Downloaded video file is bigger than remote file
            // 56 bytes for 2MP camera
            // 70 bytes for 4MP camera
            // This const was taken on runtime
            long fileSize = this.filesHelper.FileSize(path);
            return (size + 70) == fileSize || (size + 56) == fileSize;
        }
    }
}
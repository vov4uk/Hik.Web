using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HikApi.Abstraction;
using HikApi.Data;
using HikConsole.Abstraction;
using HikConsole.Config;

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
        private RemoteVideoFile currentDownloadFile;
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
            this.hikApi.Initialize();
            string sdkLogsPath = this.filesHelper.CombinePath(Environment.CurrentDirectory, "logs", this.config.Allias + "_SdkLog");
            this.filesHelper.FolderCreateIfNotExist(sdkLogsPath);
            this.hikApi.SetupLogs(3, sdkLogsPath, false);

            this.filesHelper.FolderCreateIfNotExist(this.config.DestinationFolder);
        }

        public bool Login()
        {
            if (this.session == null)
            {
                this.session = this.hikApi.Login(this.config.IpAddress, this.config.PortNumber, this.config.UserName, this.config.Password);
                this.logger.Info($"Sucessfull login to {this.config.ToString()}");
                return true;
            }
            else
            {
                this.logger.Warn("HikClient.Login : Already logged in");
                return false;
            }
        }

        public bool StartDownload(RemoteVideoFile remoteFile)
        {
            if (!this.IsDownloading)
            {
                string workingDirectory = this.GetWorkingDirectory(remoteFile);
                this.filesHelper.FolderCreateIfNotExist(workingDirectory);

                string destenationFilePath = this.GetFullPath(remoteFile, workingDirectory);

                if (!this.filesHelper.FileExists(destenationFilePath, remoteFile.Size))
                {
                    this.downloadId = this.hikApi.StartDownloadFile(this.session.UserId, remoteFile.Name, destenationFilePath);

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

        public void StopDownload()
        {
            if (this.IsDownloading)
            {
                this.hikApi.StopDownloadFile(this.downloadId);
                this.ResetDownloadStatus();
            }
            else
            {
                this.logger.Warn("HikClient.StopDownload : File not downloading now");
            }
        }

        public void UpdateProgress()
        {
            if (this.IsDownloading)
            {
                int downloadProgress = this.hikApi.GetDownloadPosition(this.downloadId);

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
            this.StopDownload();
            this.DeleteCurrentFile();
        }

        public Task<IList<RemoteVideoFile>> FindAsync(DateTime periodStart, DateTime periodEnd)
        {
            this.ValidateDateParameters(periodStart, periodEnd);

            this.logger.Info($"Get videos from {periodStart.ToString()} to {periodEnd.ToString()}");

            return this.SearchVideoFilesAsync(periodStart, periodEnd, this.session);
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

        private string GetWorkingDirectory(RemoteVideoFile file)
        {
            return this.filesHelper.CombinePath(this.config.DestinationFolder, file.ToDirectoryNameString());
        }

        private string GetFullPath(RemoteVideoFile file, string directory = null)
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
                throw new ArgumentException("Start period grather than end");
            }
        }

        private async Task<IList<RemoteVideoFile>> SearchVideoFilesAsync(DateTime periodStart, DateTime periodEnd, Session loginResult)
        {
            return await this.hikApi.FindVideoFilesAsync(periodStart, periodEnd, loginResult.UserId, loginResult.Device.StartChannel);
        }

        private void UpdateProgressInternal(int progress)
        {
            if (progress > ProgressBarMinimum && progress < ProgressBarMaximum)
            {
                this.progress?.Report((double)progress / ProgressBarMaximum);
            }
            else if (progress == ProgressBarMaximum)
            {
                this.StopDownload();
                this.currentDownloadFile = null;

                this.logger.Info("- downloaded");
            }
            else
            {
                this.StopDownload();
                throw new InvalidOperationException($"UpdateDownloadProgress failed, progress value = {progress}");
            }
        }
    }
}
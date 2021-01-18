using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Hik.Api.Abstraction;
using Hik.Api.Data;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using NLog;

namespace Hik.Client
{
    public class YiClient : IHikClient
    {
        private const string DefaultPath = "/tmp/sd/record/";
        private const string YiFilePathFormat = "yyyy'Y'MM'M'dd'D'HH'H'";
        private const string YiFileNameFormat = "mm'M00S'";
        private const string Yi720pFileNameFormat = "mm'M00S60'";
        private readonly CameraConfig config;
        private readonly IFilesHelper filesHelper;
        private FtpClient ftp;
        private ILogger logger = LogManager.GetCurrentClassLogger();
        private IHikRemoteFile currentDownloadFile;
        private bool disposedValue = false;

        public YiClient(CameraConfig config, IFilesHelper filesHelper)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config;
            this.filesHelper = filesHelper;
        }

        public bool IsDownloading => currentDownloadFile != null;

        public HdInfo CheckHardDriveStatus()
        {
            return default;
        }

        public Task<IList<RemotePhotoFile>> FindPhotosAsync(DateTime periodStart, DateTime periodEnd)
        {
            throw new NotImplementedException();
        }

        public Task<IList<RemoteVideoFile>> FindVideosAsync(DateTime periodStart, DateTime periodEnd)
        {
            var result = new List<RemoteVideoFile>();
            var end = periodEnd.AddMinutes(-1);
            var seconds = periodStart.Second;
            periodStart = periodStart.AddSeconds(-seconds);

            while (periodStart < end)
            {
                var utcStart = periodStart.ToUniversalTime();

                var file = new RemoteVideoFile()
                {
                    Name = utcStart.ToString(YiFileNameFormat),
                    FilePath = $"{DefaultPath}{utcStart.ToString(YiFilePathFormat)}/{utcStart.ToString(GetFileNameformat())}.mp4",

                    StartTime = periodStart,
                    StopTime = periodStart.AddMinutes(1),
                };
                result.Add(file);
                periodStart = periodStart.AddMinutes(1);
            }

            return Task.FromResult(result as IList<RemoteVideoFile>);
        }

        public async Task<bool> DownloadFileAsync(RemoteVideoFile remoteFile, CancellationToken token)
        {
            string destinationFilePath = GetPathSafety(remoteFile);
            var remoteFileExist = await ftp.FileExistsAsync(remoteFile.FilePath);

            if (remoteFileExist)
            {
                if (!filesHelper.FileExists(destinationFilePath))
                {
                    logger.Info($"{remoteFile.ToUserFriendlyString()}- downloading");
                    currentDownloadFile = remoteFile;
                    var tempFile = destinationFilePath + ".tmp";
                    await ftp.DownloadFileAsync(tempFile, remoteFile.FilePath, FtpLocalExists.Overwrite, FtpVerify.None, null, token);

                    currentDownloadFile = null;
                    filesHelper.RenameFile(tempFile, destinationFilePath);
                    remoteFile.Size = filesHelper.FileSize(destinationFilePath);

                    return true;
                }
                else
                {
                    logger.Info($"{remoteFile.ToUserFriendlyString()}- exist");
                    return false;
                }
            }
            else
            {
                logger.Error($"File not found {remoteFile.FilePath}");
                return false;
            }
        }

        public void ForceExit()
        {
            ftp.Disconnect();
            DeleteCurrentFile();
        }

        public void InitializeClient()
        {
            ftp = new FtpClient
            {
                Host = config.IpAddress,
                Credentials = new NetworkCredential(config.UserName, config.Password),
            };
        }

        public bool Login()
        {
            ftp.Connect();
            return true;
        }

        public bool PhotoDownload(RemotePhotoFile remoteFile)
        {
            throw new NotSupportedException();
        }

        public bool StartVideoDownload(IHikRemoteFile remoteFile)
        {
            throw new NotSupportedException();
        }

        public void StopVideoDownload()
        {
            throw new NotSupportedException();
        }

        public void UpdateVideoProgress()
        {
            throw new NotSupportedException();
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

                ftp?.Disconnect();
                ftp?.Dispose();

                currentDownloadFile = null;
                disposedValue = true;
            }
        }

        private string GetPathSafety(IHikRemoteFile remoteFile)
        {
            string workingDirectory = GetWorkingDirectory(remoteFile);
            filesHelper.FolderCreateIfNotExist(workingDirectory);

            string destinationFilePath = GetFullPath(remoteFile, workingDirectory);
            return destinationFilePath;
        }

        private string GetFileNameformat()
        {
            return config.ClientType == ClientType.Yi ? YiFileNameFormat : Yi720pFileNameFormat;
        }

        private string GetWorkingDirectory(IHikRemoteFile file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, file.ToYiDirectoryNameString());
        }

        private string GetFullPath(IHikRemoteFile file, string directory = null)
        {
            string folder = directory ?? GetWorkingDirectory(file);
            return filesHelper.CombinePath(folder, file.ToYiFileNameString());
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

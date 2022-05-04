using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using NLog;

namespace Hik.Client.Client
{
    public abstract class FtpBaseClient : IClient
    {
        protected readonly CameraConfig config;
        protected readonly IFilesHelper filesHelper;
        protected readonly IDirectoryHelper directoryHelper;
        protected readonly IFtpClient ftp;
        protected readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private bool disposedValue = false;

        protected FtpBaseClient(CameraConfig config, IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IFtpClient ftp)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
            this.ftp = ftp;
        }

        public async Task<bool> DownloadFileAsync(MediaFileDTO remoteFile, CancellationToken token)
        {
            string localFilePath = GetPathSafety(remoteFile);
            var remoteFilePath = GetRemoteFileFath(remoteFile);

            if (!filesHelper.FileExists(localFilePath))
            {
                return await DownloadInternalAsync(remoteFile, localFilePath, remoteFilePath, token);
            }
            else
            {
                LogDebugInfo($"{remoteFile.Name} - exist");
                return false;
            }
        }

        public abstract Task<IReadOnlyCollection<MediaFileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd);

        public void ForceExit()
        {
            Dispose(true);
        }

        public void InitializeClient()
        {
            ftp.Host = config.IpAddress;
            ftp.ConnectTimeout = 5 * 1000;
            ftp.DataConnectionReadTimeout = 5 * 1000;
            ftp.ReadTimeout = 5 * 1000;
            ftp.DataConnectionConnectTimeout = 5 * 1000;
            ftp.RetryAttempts = 3;
            ftp.Credentials = new NetworkCredential(config.UserName, config.Password);
        }

        public bool Login()
        {
            ftp.Connect();
            return true;
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
                LogDebugInfo("Logout the device");

                ftp?.Disconnect();
                ftp?.Dispose();

                disposedValue = true;
            }
        }

        protected async Task<bool> DownloadInternalAsync(MediaFileDTO remoteFile, string localFilePath, string remoteFilePath, CancellationToken token)
        {
            var remoteFileExist = await ftp.FileExistsAsync(remoteFilePath, token);

            if (remoteFileExist)
            {
                LogDebugInfo($"{remoteFile.Name} - downloading");
                var tempFile = filesHelper.GetTempFileName();
                await ftp.DownloadFileAsync(tempFile, remoteFilePath, FtpLocalExists.Overwrite, FtpVerify.None, null, token);

                filesHelper.RenameFile(tempFile, localFilePath);

                return await PostDownloadFileProcessAsync(remoteFile, localFilePath, remoteFilePath, token);
            }
            else
            {
                logger.Error($"{config.Alias} - File not found {remoteFilePath}");
                return false;
            }
        }

        protected abstract Task<bool> PostDownloadFileProcessAsync(MediaFileDTO remoteFile, string localFilePath, string remoteFilePath, CancellationToken token);

        protected abstract string GetRemoteFileFath(MediaFileDTO remoteFile);

        protected string GetWorkingDirectory(MediaFileDTO file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, file.ToVideoDirectoryNameString());
        }

        protected void LogDebugInfo(string msg)
        {
            logger.Debug($"{config.Alias} - {msg}");
        }

        private string GetPathSafety(MediaFileDTO remoteFile)
        {
            string workingDirectory = GetWorkingDirectory(remoteFile);
            directoryHelper.CreateDirIfNotExist(workingDirectory);

            return filesHelper.CombinePath(workingDirectory, remoteFile.ToYiFileNameString());
        }
    }
}

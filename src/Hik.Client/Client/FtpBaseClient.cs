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
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;
using Polly;

namespace Hik.Client.Client
{
    public abstract class FtpBaseClient : IClient
    {
        protected readonly CameraConfig config;
        protected readonly IFilesHelper filesHelper;
        protected readonly IDirectoryHelper directoryHelper;
        protected readonly IFtpClient ftp;
        protected readonly ILogger logger;
        private bool disposedValue = false;

        protected FtpBaseClient(
            CameraConfig config,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IFtpClient ftp,
            ILogger logger)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
            this.ftp = ftp;
            this.logger = logger;
        }

        public async Task<bool> DownloadFileAsync(MediaFileDto remoteFile, CancellationToken token)
        {
            string localFilePath = GetLocalFilePath(remoteFile);
            string remoteFilePath = GetRemoteFilePath(remoteFile);

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

        public abstract Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd);

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

        protected async Task<bool> DownloadInternalAsync(MediaFileDto remoteFile, string localFilePath, string remoteFilePath, CancellationToken token)
        {
            var remoteFileExist = await ftp.FileExistsAsync(remoteFilePath, token);

            if (remoteFileExist)
            {
                LogDebugInfo($"{remoteFile.Name} - downloading");
                var tempFile = filesHelper.GetTempFileName();

                await Policy
                    .Handle<FtpException>()
                    .Or<TimeoutException>()
                    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(3))
                    .ExecuteAsync(() => ftp.DownloadFileAsync(tempFile, remoteFilePath, FtpLocalExists.Overwrite, FtpVerify.None, null, token));

                filesHelper.RenameFile(tempFile, localFilePath);

                return await PostDownloadFileProcessAsync(remoteFile, localFilePath, remoteFilePath, token);
            }
            else
            {
                logger.LogError($"{config.Alias} - File not found {remoteFilePath}");
                return false;
            }
        }

        protected abstract Task<bool> PostDownloadFileProcessAsync(MediaFileDto remoteFile, string localFilePath, string remoteFilePath, CancellationToken token);

        protected abstract string GetRemoteFilePath(MediaFileDto remoteFile);

        protected abstract string GetLocalFilePath(MediaFileDto remoteFile);

        protected string GetWorkingDirectory(MediaFileDto file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, file.ToVideoDirectoryNameString());
        }

        protected void LogDebugInfo(string msg)
        {
            logger.LogDebug($"{config.Alias} - {msg}");
        }
    }
}

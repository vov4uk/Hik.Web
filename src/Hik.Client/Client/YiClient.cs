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

namespace Hik.Client
{
    public class YiClient : IClient
    {
        private const string YiFileNameFormat = "mm'M00S'";

        private readonly CameraConfig config;
        private readonly IFilesHelper filesHelper;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IFtpClient ftp;
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private bool disposedValue = false;

        public YiClient(CameraConfig config, IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IFtpClient ftp)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
            this.ftp = ftp;
        }

        public async Task<bool> DownloadFileAsync(MediaFileDTO remoteFile, CancellationToken token)
        {
            string destinationFilePath = GetPathSafety(remoteFile);
            var filePath = remoteFile.Date.ToYiFilePathString(config.ClientType);

            if (!filesHelper.FileExists(destinationFilePath))
            {
                return await DownloadInternalAsync(remoteFile, destinationFilePath, filePath, token);
            }
            else
            {
                LogDebugInfo($"{remoteFile.ToVideoUserFriendlyString()} - exist");
                return false;
            }
        }

        public Task<IReadOnlyCollection<MediaFileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            var result = new List<MediaFileDTO>();
            periodStart = new DateTime(periodStart.Year, periodStart.Month, periodStart.Day, periodStart.Hour, periodStart.Minute, 0, 0);
            var end = periodEnd.AddMinutes(-1);

            while (periodStart < end)
            {
                var file = new MediaFileDTO()
                {
                    Name = periodStart.ToUniversalTime().ToString(YiFileNameFormat),
                    Date = periodStart,
                    Duration = 60,
                };
                result.Add(file);
                periodStart = periodStart.AddMinutes(1);
            }

            return Task.FromResult(result.AsReadOnly() as IReadOnlyCollection<MediaFileDTO>);
        }

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

        private async Task<bool> DownloadInternalAsync(MediaFileDTO remoteFile, string targetFilePath, string remoteFilePath, CancellationToken token)
        {
            var remoteFileExist = await ftp.FileExistsAsync(remoteFilePath, token);

            if (remoteFileExist)
            {
                LogDebugInfo($"{remoteFile.ToVideoUserFriendlyString()} - downloading");
                var tempFile = filesHelper.GetTempFileName();
                await ftp.DownloadFileAsync(tempFile, remoteFilePath, FtpLocalExists.Overwrite, FtpVerify.None, null, token);

                filesHelper.RenameFile(tempFile, targetFilePath);
                remoteFile.Size = filesHelper.FileSize(targetFilePath);
                remoteFile.Path = targetFilePath;
                return true;
            }
            else
            {
                logger.Error($"{config.Alias} - File not found {remoteFilePath}");
                return false;
            }
        }

        private string GetPathSafety(MediaFileDTO remoteFile)
        {
            string workingDirectory = GetWorkingDirectory(remoteFile);
            directoryHelper.CreateDirIfNotExist(workingDirectory);

            return filesHelper.CombinePath(workingDirectory, remoteFile.ToYiFileNameString());
        }

        private string GetWorkingDirectory(MediaFileDTO file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, file.ToVideoDirectoryNameString());
        }

        private void LogDebugInfo(string msg)
        {
            logger.Debug($"{config.Alias} - {msg}");
        }
    }
}

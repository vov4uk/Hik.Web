using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using NLog;

namespace Hik.Client
{
    public class RSyncClient : IClient
    {
        private readonly CameraConfig config;
        private readonly IFilesHelper filesHelper;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IFtpClient ftp;
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private bool disposedValue = false;

        public RSyncClient(CameraConfig config, IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IFtpClient ftp)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
            this.ftp = ftp;
        }

        public async Task<bool> DownloadFileAsync(MediaFileDTO remoteFile, CancellationToken token)
        {
            string destinationFilePath = GetPathSafety(remoteFile);
            var filePath = remoteFile.Path;

            if (!filesHelper.FileExists(destinationFilePath))
            {
                return await DownloadInternalAsync(remoteFile, destinationFilePath, filePath, token);
            }
            else
            {
                LogDebugInfo($"{remoteFile.Name} - exist");
                return false;
            }
        }

        public async Task<IReadOnlyCollection<MediaFileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            FtpListItem[] filesFromFtp = await ftp.GetListingAsync($"/{config.Alias.Split(".")[1]}");

            var files = filesFromFtp.Select(item => new MediaFileDTO
            {
                Name = item.Name,
                Path = item.FullName,
                Date = item.Modified.ToLocalTime(),
                Size = item.Size,
                Duration = 1,
            }).ToList();

            return files.AsReadOnly();
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
                LogDebugInfo($"{remoteFile.Name} - downloading");
                var tempFile = filesHelper.GetTempFileName();
                await ftp.DownloadFileAsync(tempFile, remoteFilePath, FtpLocalExists.Overwrite, FtpVerify.None, null, token);

                filesHelper.RenameFile(tempFile, targetFilePath);
                var size = filesHelper.FileSize(targetFilePath);
                if (size == remoteFile.Size)
                {
                    remoteFile.Path = targetFilePath;
                    try
                    {
                        await ftp.DeleteFileAsync(remoteFilePath);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        return false;
                    }

                    return true;
                }

                return false;
            }
            else
            {
                logger.Error($"{config.Alias} - File not found {remoteFilePath}");
                return false;
            }
        }

        private string GetPathSafety(MediaFileDTO remoteFile)
        {
            string workingDirectory = config.DestinationFolder;
            directoryHelper.CreateDirIfNotExist(workingDirectory);

            return filesHelper.CombinePath(workingDirectory, remoteFile.Name);
        }

        private void LogDebugInfo(string msg)
        {
            logger.Debug($"{config.Alias} - {msg}");
        }
    }
}

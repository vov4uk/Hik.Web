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
        private FtpClient ftp;
        private ILogger logger = LogManager.GetCurrentClassLogger();
        private FileDTO currentDownloadFile;
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

        public async Task<bool> DownloadFileAsync(FileDTO remoteFile, CancellationToken token)
        {
            string destinationFilePath = GetPathSafety(remoteFile);
            var filePath = remoteFile.Date.ToYiFilePathString(config.ClientType);
            var remoteFileExist = await ftp.FileExistsAsync(filePath);

            if (remoteFileExist)
            {
                if (!filesHelper.FileExists(destinationFilePath))
                {
                    logger.Info($"{remoteFile.ToVideoUserFriendlyString()}- downloading");
                    currentDownloadFile = remoteFile;
                    var tempFile = destinationFilePath + ".tmp";
                    await ftp.DownloadFileAsync(tempFile, filePath, FtpLocalExists.Overwrite, FtpVerify.None, null, token);

                    currentDownloadFile = null;
                    filesHelper.RenameFile(tempFile, destinationFilePath);
                    remoteFile.Size = filesHelper.FileSize(destinationFilePath);

                    return true;
                }
                else
                {
                    logger.Info($"{remoteFile.ToVideoUserFriendlyString()}- exist");
                    return false;
                }
            }
            else
            {
                logger.Error($"File not found {filePath}");
                return false;
            }
        }

        public Task<IList<FileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            var result = new List<FileDTO>();
            var end = periodEnd.AddMinutes(-1);
            var seconds = periodStart.Second;
            periodStart = periodStart.AddSeconds(-seconds);

            while (periodStart < end)
            {
                var file = new FileDTO()
                {
                    Name = periodStart.ToUniversalTime().ToString(YiFileNameFormat),
                    Date = periodStart,
                    Duration = 60,
                };
                result.Add(file);
                periodStart = periodStart.AddMinutes(1);
            }

            return Task.FromResult(result as IList<FileDTO>);
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

        private string GetPathSafety(FileDTO remoteFile)
        {
            string workingDirectory = GetWorkingDirectory(remoteFile);
            filesHelper.FolderCreateIfNotExist(workingDirectory);

            string destinationFilePath = GetFullPath(remoteFile, workingDirectory);
            return destinationFilePath;
        }

        private string GetWorkingDirectory(FileDTO file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, file.ToYiDirectoryNameString());
        }

        private string GetFullPath(FileDTO file, string directory = null)
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

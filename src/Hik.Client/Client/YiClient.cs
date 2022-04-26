using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Hik.Client.Abstraction;
using Hik.Client.Client;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client
{
    public class YiClient : FtpBaseClient
    {
        private const string YiFileNameFormat = "mm'M00S'";

        public YiClient(CameraConfig config, IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IFtpClient ftp)
            : base(config, filesHelper, directoryHelper, ftp)
        {
        }

        public override async Task<bool> DownloadFileAsync(MediaFileDTO remoteFile, CancellationToken token)
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

        public override Task<IReadOnlyCollection<MediaFileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
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
    }
}

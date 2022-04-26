using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Hik.Client.Abstraction;
using Hik.Client.Client;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client
{
    public class RSyncClient : FtpBaseClient
    {
        public RSyncClient(CameraConfig config, IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IFtpClient ftp)
            : base(config, filesHelper, directoryHelper, ftp)
        {
        }

        public override async Task<bool> DownloadFileAsync(MediaFileDTO remoteFile, CancellationToken token)
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

        public override async Task<IReadOnlyCollection<MediaFileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
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
    }
}

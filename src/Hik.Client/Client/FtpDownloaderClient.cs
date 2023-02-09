using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Exceptions;
using Hik.Client.Client;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;

namespace Hik.Client
{
    public class FtpDownloaderClient : FtpDownloaderClientBase
    {
        public FtpDownloaderClient(
            CameraConfig config,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IAsyncFtpClient ftp,
            ILogger logger)
            : base(config, filesHelper, directoryHelper, ftp, logger)
        {
        }

        public override async Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            string path = !string.IsNullOrEmpty(config.RemotePath) ? config.RemotePath : $"/{config.Alias.Split(".")[1]}";

            FtpListItem[] filesFromFtp = await ftp.GetListing(path);

            var files = filesFromFtp.Select(item => new MediaFileDto
            {
                Name = item.Name,
                Path = item.FullName,
                Date = item.Modified.ToLocalTime(),
                Size = item.Size,
                Duration = 1,
            }).ToList();

            return files.AsReadOnly();
        }

        protected override string GetRemoteFilePath(MediaFileDto remoteFile)
            => remoteFile.Path;

        protected override string GetLocalFilePath(MediaFileDto remoteFile)
            => filesHelper.CombinePath(config.DestinationFolder, remoteFile.Name);

        protected override async Task<bool> PostDownloadFileProcessAsync(MediaFileDto remoteFile, string localFilePath, string remoteFilePath, CancellationToken token)
        {
            var size = filesHelper.FileSize(localFilePath);
            if (size == remoteFile.Size)
            {
                remoteFile.Path = localFilePath;
                try
                {
                    await ftp.DeleteFile(remoteFilePath);
                }
                catch (FtpException ex)
                {
                    logger.LogError(ex, "Failed to delete remote file");
                    return false;
                }

                return true;
            }

            return false;
        }
    }
}

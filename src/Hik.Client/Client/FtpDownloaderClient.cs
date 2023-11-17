using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Hik.Client.Client;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;

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
            FtpListItem[] filesFromFtp = await ftp.GetListing(config.RemotePath);

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
                await ftp.DeleteFile(remoteFilePath, token);
                return true;
            }

            return false;
        }
    }
}

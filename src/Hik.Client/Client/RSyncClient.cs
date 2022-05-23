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

namespace Hik.Client
{
    public class RSyncClient : FtpBaseClient
    {
        public RSyncClient(CameraConfig config, IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IFtpClient ftp)
            : base(config, filesHelper, directoryHelper, ftp)
        {
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

        protected override string GetRemoteFilePath(MediaFileDTO remoteFile)
            => remoteFile.Path;

        protected override string GetLocalFilePath(MediaFileDTO remoteFile)
            => filesHelper.CombinePath(config.DestinationFolder, remoteFile.Name);

        protected override async Task<bool> PostDownloadFileProcessAsync(MediaFileDTO remoteFile, string localFilePath, string remoteFilePath, CancellationToken token)
        {
            var size = filesHelper.FileSize(localFilePath);
            if (size == remoteFile.Size)
            {
                remoteFile.Path = localFilePath;
                try
                {
                    await ftp.DeleteFileAsync(remoteFilePath, token);
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
    }
}

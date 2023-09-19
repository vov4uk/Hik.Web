using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using FluentFTP.Exceptions;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;

namespace Hik.Client.Client
{
    public abstract class FtpDownloaderClientBase : FtpClientBase, IDownloaderClient
    {
        protected readonly CameraConfig config;
        protected readonly IFilesHelper filesHelper;
        protected readonly IDirectoryHelper directoryHelper;

        protected FtpDownloaderClientBase(
            CameraConfig config,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IAsyncFtpClient ftp,
            ILogger logger)
            : base(config?.Camera, ftp, logger)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
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
                logger.Information($"{remoteFile.Name} - exist");
                return false;
            }
        }

        public abstract Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd);

        protected async Task<bool> DownloadInternalAsync(MediaFileDto remoteFile, string localFilePath, string remoteFilePath, CancellationToken token)
        {
            var remoteFileExist = await ftp.FileExists(remoteFilePath, token);

            if (remoteFileExist)
            {
                try
                {
                    logger.Debug($"{remoteFile.Name} - downloading");
                    var tempFile = filesHelper.GetTempFileName();

                    await ftp.DownloadFile(tempFile, remoteFilePath, FtpLocalExists.Skip, FtpVerify.None, null, token);

                    filesHelper.RenameFile(tempFile, localFilePath);

                    return await PostDownloadFileProcessAsync(remoteFile, localFilePath, remoteFilePath, token);
                }
                catch (FtpException ex)
                {
                    this.logger.Error("ErrorMsg: {errorMsg}; Trace: {trace}", ex.InnerException?.Message, ex.InnerException?.ToStringDemystified());
                    return false;
                }
            }
            else
            {
                logger.Warning($"File not found {remoteFilePath}");
                return false;
            }
        }

        protected abstract Task<bool> PostDownloadFileProcessAsync(MediaFileDto remoteFile, string localFilePath, string remoteFilePath, CancellationToken token);

        protected abstract string GetRemoteFilePath(MediaFileDto remoteFile);

        protected abstract string GetLocalFilePath(MediaFileDto remoteFile);

        protected string GetWorkingDirectory(MediaFileDto file)
        {
            return filesHelper.CombinePath(config.DestinationFolder, file.Date.ToDirectoryName());
        }
    }
}

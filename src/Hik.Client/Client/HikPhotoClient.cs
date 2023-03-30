using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api;
using Hik.Api.Abstraction;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;

namespace Hik.Client
{
    public sealed class HikPhotoClient : HikBaseClient, IDownloaderClient
    {
        private readonly IImageHelper imageHelper;

        public HikPhotoClient(
            CameraConfig config,
            IHikApi hikApi,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper,
            IImageHelper imageHelper,
            ILogger logger)
            : base(config, hikApi, filesHelper, directoryHelper, mapper, logger)
        {
            this.imageHelper = imageHelper;
        }

        public Task<bool> DownloadFileAsync(MediaFileDto remoteFile, CancellationToken token)
        {
            try
            {
                string targetFilePath = GetPathSafety(remoteFile);

                if (!filesHelper.FileExists(targetFilePath))
                {
                    string tempFile = ToFileNameString(remoteFile);
                    hikApi.PhotoService.DownloadFile(session.UserId, remoteFile.Name, remoteFile.Size, tempFile);

                    filesHelper.RenameFile(tempFile, targetFilePath);
                    this.imageHelper.SetDate(targetFilePath, remoteFile.Date);

                    remoteFile.Path = targetFilePath;

                    return Task.FromResult(true);
                }
            }
            catch (HikException e)
            {
                var msg = $"Failed to download {remoteFile.Name} : {remoteFile.Path}. Code : {e.ErrorCode}; {e.ErrorMessage}";
                logger.Error(e, msg);
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to download {remoteFile.Name} : {remoteFile.Path}");
            }

            return Task.FromResult(false);
        }

        public async Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            logger.Information($"Get photos from {periodStart} to {periodEnd}");

            var remoteFiles = await hikApi.PhotoService.FindFilesAsync(periodStart, periodEnd, session);

            return Mapper.Map<IReadOnlyCollection<MediaFileDto>>(remoteFiles);
        }

        protected override void StopDownload()
        {
        }

        protected override string ToFileNameString(MediaFileDto file)
        {
            return file.ToPhotoFileNameString();
        }

        protected override string ToDirectoryNameString(MediaFileDto file)
        {
            return config.SaveFilesToRootFolder ? string.Empty : file.Date.ToPhotoDirectoryNameString();
        }
    }
}

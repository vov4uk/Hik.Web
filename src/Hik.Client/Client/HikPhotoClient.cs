using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;

namespace Hik.Client
{
    public sealed class HikPhotoClient : HikBaseClient
    {
        private readonly IImageHelper imageHelper;

        public HikPhotoClient(
            CameraConfig config,
            IHikSDK hikSDK,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper,
            IImageHelper imageHelper,
            ILogger logger)
            : base(config, hikSDK, filesHelper, directoryHelper, mapper, logger)
        {
            this.imageHelper = imageHelper;
        }

        public override async Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            logger.Information($"Get photos from {periodStart} to {periodEnd}");

            var remoteFiles = await hikApi.PhotoService.FindFilesAsync(periodStart, periodEnd);

            return Mapper.Map<IReadOnlyCollection<MediaFileDto>>(remoteFiles);
        }

        public override Task<bool> DownloadFileAsync(MediaFileDto remoteFile, CancellationToken token)
        {
            string targetFilePath = GetPathSafety(remoteFile);

            if (!filesHelper.FileExists(targetFilePath))
            {
                string tempFile = ToFileNameString(remoteFile);
                hikApi.PhotoService.DownloadFile(remoteFile.Name, remoteFile.Size, tempFile);

                filesHelper.RenameFile(tempFile, targetFilePath);
                this.imageHelper.SetDate(targetFilePath, remoteFile.Date);

                remoteFile.Path = targetFilePath;

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
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
            return config.SaveFilesToRootFolder ? string.Empty : file.Date.ToDirectoryName();
        }
    }
}

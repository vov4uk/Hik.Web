using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client
{
    public sealed class HikPhotoClient : HikBaseClient, IClient
    {
        private readonly IImageHelper imageHelper;

        public HikPhotoClient(CameraConfig config, IHikApi hikApi, IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IMapper mapper, IImageHelper imageHelper)
            : base(config, hikApi, filesHelper, directoryHelper, mapper)
        {
            this.imageHelper = imageHelper;
        }

        public Task<bool> DownloadFileAsync(MediaFileDTO remoteFile, CancellationToken token)
        {
            string targetFilePath = GetPathSafety(remoteFile);

            if (!filesHelper.FileExists(targetFilePath))
            {
                string tempFile = ToFileNameString(remoteFile);
                hikApi.PhotoService.DownloadFile(session.UserId, remoteFile.Name, remoteFile.Size, tempFile);

                this.imageHelper.SetDate(tempFile, targetFilePath, remoteFile.Date);
                filesHelper.DeleteFile(tempFile);
                remoteFile.Path = targetFilePath;

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public async Task<IReadOnlyCollection<MediaFileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            logger.Info($"{config.Alias} - Get photos from {periodStart} to {periodEnd}");

            var remoteFiles = await hikApi.PhotoService.FindFilesAsync(periodStart, periodEnd, session);

            return Mapper.Map<IReadOnlyCollection<MediaFileDTO>>(remoteFiles);
        }

        protected override void StopDownload()
        {
        }

        protected override string ToFileNameString(MediaFileDTO file)
        {
            return file.ToPhotoFileNameString();
        }

        protected override string ToDirectoryNameString(MediaFileDTO file)
        {
            return file.Date.ToPhotoDirectoryNameString();
        }
    }
}

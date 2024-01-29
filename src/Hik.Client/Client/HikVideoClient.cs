using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Api.Data;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;

namespace Hik.Client
{
    public sealed class HikVideoClient : HikBaseClient
    {
        private const int ProgressCheckPeriodMilliseconds = 5000;

        public HikVideoClient(
            CameraConfig config,
            IHikSDK hikSDK,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper,
            ILogger logger)
            : base(config, hikSDK, filesHelper, directoryHelper, mapper, logger)
        {
        }

        private bool IsDownloading => downloadId >= 0;

        public override async Task<bool> DownloadFileAsync(MediaFileDto remoteFile, CancellationToken token)
        {
            string targetFilePath = GetPathSafety(remoteFile);
            string tempFile = filesHelper.GetTempFileName() + ".mp4";
            if (this.StartVideoDownload(remoteFile, targetFilePath, tempFile))
            {
                do
                {
                    await Task.Delay(ProgressCheckPeriodMilliseconds, token);
                    token.ThrowIfCancellationRequested();
                    this.UpdateVideoProgress();
                }
                while (this.IsDownloading);

                filesHelper.RenameFile(tempFile, targetFilePath);
                remoteFile.Size = filesHelper.FileSize(targetFilePath);
                remoteFile.Path = targetFilePath;

                return true;
            }

            return false;
        }

        public override async Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            logger.Information($"Get videos from {periodStart} to {periodEnd}");

            IReadOnlyCollection<HikRemoteFile> remoteFiles = await hikApi.VideoService.FindFilesAsync(periodStart, periodEnd);
            return Mapper.Map<IReadOnlyCollection<MediaFileDto>>(remoteFiles);
        }

        protected override void StopDownload()
        {
            if (IsDownloading)
            {
                hikApi.VideoService.StopDownloadFile(downloadId);
                ResetDownloadStatus();
            }
            else
            {
                logger.Warning("File not downloading now");
            }
        }

        protected override string ToDirectoryNameString(MediaFileDto file)
        {
            return config.SaveFilesToRootFolder ? string.Empty : file.Date.ToDirectoryName();
        }

        protected override string ToFileNameString(MediaFileDto file)
        {
            return file.ToVideoFileNameString();
        }

        private bool StartVideoDownload(MediaFileDto file, string targetFilePath, string tempFile)
        {
            if (!IsDownloading)
            {
                if (!filesHelper.FileExists(targetFilePath))
                {
                    downloadId = hikApi.VideoService.StartDownloadFile(file.Name, tempFile);

                    logger.Information($"{file.ToVideoUserFriendlyString()} - downloading");
                    return true;
                }

                logger.Warning($"{file.ToVideoUserFriendlyString()} - exist");
            }
            else
            {
                logger.Warning("Downloading, please stop firstly!");
            }

            return false;
        }

        private void UpdateProgressInternal(int progressValue)
        {
            if (progressValue == ProgressBarMaximum)
            {
                StopDownload();
                logger.Debug("Downloaded");
            }
            else if (progressValue < ProgressBarMinimum || progressValue > ProgressBarMaximum)
            {
                StopDownload();
                throw new InvalidOperationException($"UpdateDownloadProgress failed, progress value = {progressValue}");
            }
        }

        private void UpdateVideoProgress()
        {
            if (IsDownloading)
            {
                int downloadProgress = hikApi.VideoService.GetDownloadPosition(downloadId);

                UpdateProgressInternal(downloadProgress);
            }
            else
            {
                logger.Warning("File not downloading now");
            }
        }
    }
}

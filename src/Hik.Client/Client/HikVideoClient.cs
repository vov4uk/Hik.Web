﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;

namespace Hik.Client
{
    public sealed class HikVideoClient : HikBaseClient, IClient
    {
        private const int ProgressCheckPeriodMilliseconds = 5000;

        public HikVideoClient(
            CameraConfig config,
            IHikApi hikApi,
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IMapper mapper,
            ILogger logger)
            : base(config, hikApi, filesHelper, directoryHelper, mapper, logger)
        {
        }

        private bool IsDownloading => downloadId >= 0;

        public async Task<bool> DownloadFileAsync(MediaFileDto remoteFile, CancellationToken token)
        {
            string targetFilePath = GetPathSafety(remoteFile);
            string tempFile = filesHelper.GetTempFileName();
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

        public async Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            LogDebugInfo($"Get videos from {periodStart} to {periodEnd}");

            var remoteFiles = await hikApi.VideoService.FindFilesAsync(periodStart, periodEnd, session);
            return Mapper.Map<IReadOnlyCollection<MediaFileDto>>(remoteFiles);
        }

        protected override string ToFileNameString(MediaFileDto file)
        {
            return file.ToVideoFileNameString();
        }

        protected override string ToDirectoryNameString(MediaFileDto file)
        {
            return file.ToVideoDirectoryNameString();
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
                logger.LogWarning("HikVideoClient.StopDownload : File not downloading now");
            }
        }

        private bool StartVideoDownload(MediaFileDto file, string targetFilePath, string tempFile)
        {
            if (!IsDownloading)
            {
                if (!filesHelper.FileExists(targetFilePath))
                {
                    LogDebugInfo($"{targetFilePath}");
                    downloadId = hikApi.VideoService.StartDownloadFile(session.UserId, file.Name, tempFile);

                    LogDebugInfo($"{file.ToVideoUserFriendlyString()} - downloading");
                    return true;
                }

                LogDebugInfo($"{file.ToVideoUserFriendlyString()} - exist");
                return false;
            }
            else
            {
                logger.LogWarning("HikVideoClient.StartDownload : Downloading, please stop firstly!");
                return false;
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
                logger.LogWarning("HikVideoClient.UpdateProgress : File not downloading now");
            }
        }

        private void UpdateProgressInternal(int progressValue)
        {
            if (progressValue == ProgressBarMaximum)
            {
                StopDownload();
                LogDebugInfo(" - downloaded");
            }
            else if (progressValue < ProgressBarMinimum || progressValue > ProgressBarMaximum)
            {
                StopDownload();
                throw new InvalidOperationException($"HikVideoClient.UpdateDownloadProgress failed, progress value = {progressValue}");
            }
        }

        private void LogDebugInfo(string msg)
        {
            logger.LogDebug($"{config.Alias} - {msg}");
        }
    }
}

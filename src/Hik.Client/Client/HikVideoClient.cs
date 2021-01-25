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
    public class HikVideoClient : HikBaseClient, IClient
    {
        protected const string DurationFormat = "h'h 'm'm 's's'";

        public HikVideoClient(CameraConfig config, IHikApi hikApi, IFilesHelper filesHelper, IMapper mapper)
            : base(config, hikApi, filesHelper, mapper)
        {
        }

        public int ProgressCheckPeriodMilliseconds { get; set; } = 5000;

        public async Task<bool> DownloadFileAsync(MediaFileDTO remoteFile, CancellationToken token)
        {
            string targetFilePath = GetPathSafety(remoteFile);
            string tempFile = targetFilePath + ".tmp";
            if (this.StartVideoDownload(remoteFile, targetFilePath, tempFile))
            {
                do
                {
                    await Task.Delay(this.ProgressCheckPeriodMilliseconds);
                    token.ThrowIfCancellationRequested();
                    this.UpdateVideoProgress();
                }
                while (this.IsDownloading);

                filesHelper.RenameFile(tempFile, targetFilePath);
                remoteFile.Size = filesHelper.FileSize(targetFilePath);

                return true;
            }

            return false;
        }

        public async Task<IList<MediaFileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            LogInfo($"Get videos from {periodStart} to {periodEnd}");

            var remoteFiles = await hikApi.VideoService.FindFilesAsync(periodStart, periodEnd, session);
            return Mapper.Map<IList<MediaFileDTO>>(remoteFiles);
        }

        protected bool StartVideoDownload(MediaFileDTO file, string targetFilePath, string tempFile)
        {
            if (!IsDownloading)
            {
                if (!filesHelper.FileExists(targetFilePath))
                {
                    LogInfo($"{targetFilePath}");
                    downloadId = hikApi.VideoService.StartDownloadFile(session.UserId, file.Name, tempFile);

                    LogInfo($"{file.ToVideoUserFriendlyString()}- downloading");

                    currentDownloadFile = file;
                    return true;
                }

                LogInfo($"{file.ToVideoUserFriendlyString()}- exist");
                return false;
            }
            else
            {
                logger.Warn("HikClient.StartDownload : Downloading, please stop firstly!");
                return false;
            }
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
                logger.Warn("HikClient.StopDownload : File not downloading now");
            }
        }

        protected void UpdateVideoProgress()
        {
            if (IsDownloading)
            {
                int downloadProgress = hikApi.VideoService.GetDownloadPosition(downloadId);

                UpdateProgressInternal(downloadProgress);
            }
            else
            {
                logger.Warn("HikClient.UpdateProgress : File not downloading now");
            }
        }

        protected override string ToFileNameString(MediaFileDTO file)
        {
            return file.ToVideoFileNameString();
        }

        protected override string ToDirectoryNameString(MediaFileDTO file)
        {
            return file.ToVideoDirectoryNameString();
        }

        private void UpdateProgressInternal(int progressValue)
        {
            if (progressValue == ProgressBarMaximum)
            {
                StopDownload();
                currentDownloadFile = null;

                LogInfo("- downloaded");
            }
            else if (progressValue < ProgressBarMinimum || progressValue > ProgressBarMaximum)
            {
                StopDownload();
                throw new InvalidOperationException($"HikClient.UpdateDownloadProgress failed, progress value = {progressValue}");
            }
        }

        private void LogInfo(string msg)
        {
            logger.Info($"{config.Alias} - {msg}");
        }
    }
}

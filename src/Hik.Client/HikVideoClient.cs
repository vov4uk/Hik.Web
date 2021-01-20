using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Api.Data;
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

        public async Task<bool> DownloadFileAsync(FileDTO remoteFile, CancellationToken token)
        {
            if (this.StartVideoDownload(remoteFile))
            {
                do
                {
                    await Task.Delay(this.ProgressCheckPeriodMilliseconds);
                    token.ThrowIfCancellationRequested();
                    this.UpdateVideoProgress();
                }
                while (this.IsDownloading);

                return true;
            }

            return false;
        }

        public async Task<IList<FileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd)
        {
            ValidateDateParameters(periodStart, periodEnd);

            logger.Info($"Get videos from {periodStart} to {periodEnd}");

            var remoteFiles = await hikApi.VideoService.FindFilesAsync(periodStart, periodEnd, session);
            return Mapper.Map<IList<FileDTO>>(remoteFiles);
        }

        protected bool StartVideoDownload(FileDTO file)
        {
            if (!IsDownloading)
            {
                string destinationFilePath = GetPathSafety(file);

                if (!CheckLocalVideoExist(destinationFilePath, file.Size))
                {
                    logger.Info($"{destinationFilePath}");
                    downloadId = hikApi.VideoService.StartDownloadFile(session.UserId, file.Name, destinationFilePath);

                    logger.Info($"{file.ToVideoUserFriendlyString()}- downloading");

                    currentDownloadFile = file;
                    return true;
                }

                logger.Info($"{file.ToVideoUserFriendlyString()}- exist");
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

        protected override string ToFileNameString(FileDTO file)
        {
            return file.ToVideoFileNameString();
        }

        protected override string ToDirectoryNameString(FileDTO file)
        {
            return file.ToVideoDirectoryNameString();
        }

        private void UpdateProgressInternal(int progressValue)
        {
            if (progressValue == ProgressBarMaximum)
            {
                StopDownload();
                currentDownloadFile = null;

                logger.Info("- downloaded");
            }
            else if (progressValue < ProgressBarMinimum || progressValue > ProgressBarMaximum)
            {
                StopDownload();
                throw new InvalidOperationException($"HikClient.UpdateDownloadProgress failed, progress value = {progressValue}");
            }
        }

        private bool CheckLocalVideoExist(string path, long size)
        {
            // Downloaded video file is 40 bytes bigger than remote file
            // This const was taken on debug
            return filesHelper.FileExists(path, size + 40);
        }
    }
}

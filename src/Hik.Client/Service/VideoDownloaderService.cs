﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Abstraction.Services;
using Hik.Client.Events;
using Hik.Client.Helpers;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Serilog;

namespace Hik.Client.Service
{
    public class VideoDownloaderService : HikDownloaderServiceBase, IHikVideoDownloaderService
    {
        public VideoDownloaderService(IDirectoryHelper directoryHelper, IClientFactory clientFactory, ILogger logger)
            : base(directoryHelper, clientFactory, logger)
        {
        }

        public override async Task<IReadOnlyCollection<MediaFileDto>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDto> remoteFiles, CancellationToken token)
        {
            int j = 1;
            foreach (var video in remoteFiles)
            {
                ThrowIfCancellationRequested();
                logger.Debug($"{j++,2}/{remoteFiles.Count} : {video.Name}");
                bool downloaded = await DownloadRemoteVideoFileAsync(video, token);
                if (downloaded)
                {
                    OnFileDownloaded(new FileDownloadedEventArgs(video));
                }
            }

            return remoteFiles;
        }

        public override async Task<IReadOnlyCollection<MediaFileDto>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            return (await this.Client.GetFilesListAsync(periodStart, periodEnd)).SkipLast(1).ToList();
        }

        private async Task<bool> DownloadRemoteVideoFileAsync(MediaFileDto file, CancellationToken token)
        {
            DateTime start = DateTime.Now;
            bool downloaded = await Client.DownloadFileAsync(file, token);
            if (downloaded)
            {
                file.DownloadStarted = start;
                var finish = DateTime.Now;
                var duration = (int?)(finish - start).TotalSeconds;
                file.DownloadDuration = duration;

                int? videoDuration = file.Duration;
                logger.Debug($"Duration {duration.FormatSeconds()}, avg speed {((long)Utils.SafeDivision(file.Size, duration.Value)).FormatBytes()}/s");
                logger.Debug($"Video    {videoDuration.FormatSeconds()}, avg rate {((long)Utils.SafeDivision(file.Size, videoDuration.Value)).FormatBytes()}/s");
            }

            return downloaded;
        }
    }
}

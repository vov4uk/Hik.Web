using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.Client.Helpers;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public class HikVideoDownloaderService : HikDownloaderServiceBase<MediaFileDTO>
    {
        public HikVideoDownloaderService(IDirectoryHelper directoryHelper, IClientFactory clientFactory, IMapper mapper)
            : base(directoryHelper, clientFactory, mapper)
        {
        }

        public override async Task<IReadOnlyCollection<MediaFileDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDTO> remoteFiles, CancellationToken token = default)
        {
            int j = 1;
            foreach (var video in remoteFiles)
            {
                this.ThrowIfCancellationRequested();
                this.logger.Info($"{j++,2}/{remoteFiles.Count} : ");
                if (await this.DownloadRemoteVideoFileAsync(video, token))
                {
                    this.OnFileDownloaded(new FileDownloadedEventArgs(video));
                }
            }

            return remoteFiles;
        }

        public override async Task<IReadOnlyCollection<MediaFileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<MediaFileDTO> videos = (await this.Client.GetFilesListAsync(periodStart, periodEnd)).SkipLast(1).ToList();

            this.logger.Info($"Video searching finished. Found {videos.Count} files");
            return videos;
        }

        private async Task<bool> DownloadRemoteVideoFileAsync(MediaFileDTO file, CancellationToken token)
        {
            DateTime start = DateTime.Now;

            if (await this.Client.DownloadFileAsync(file, token))
            {
                file.DownloadStarted = start;
                var finish = DateTime.Now;
                var duration = (int?)(finish - start).TotalSeconds;
                file.DownloadDuration = duration;

                int? videoDutation = file.Duration;
                this.logger.Info($"Duration {duration.FormatSeconds()}, avg speed {Utils.FormatBytes((long)(file.Size / duration))}/s");
                this.logger.Info($"Video    {videoDutation.FormatSeconds()}, avg rate {Utils.FormatBytes((long)(file.Size / videoDutation))}/s");
                return true;
            }

            return false;
        }
    }
}

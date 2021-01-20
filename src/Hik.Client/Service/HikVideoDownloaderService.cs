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
    public class HikVideoDownloaderService : HikDownloaderServiceBase<FileDTO>
    {
        public HikVideoDownloaderService(IDirectoryHelper directoryHelper, IClientFactory clientFactory, IMapper mapper)
            : base(directoryHelper, clientFactory, mapper)
        {
        }

        public int ProgressCheckPeriodMilliseconds { get; set; } = 5000;

        public override async Task<IReadOnlyCollection<FileDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<FileDTO> remoteFiles, CancellationToken token = default)
        {
            int j = 1;
            foreach (var video in remoteFiles)
            {
                this.ThrowIfCancellationRequested();
                this.Logger.Info($"{j++,2}/{remoteFiles.Count} : ");
                if (await this.DownloadRemoteVideoFileAsync(video, token))
                {
                    this.OnFileDownloaded(new FileDownloadedEventArgs(video));
                }
            }

            return remoteFiles;
        }

        public override async Task<IReadOnlyCollection<FileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<FileDTO> videos = (await this.Client.GetFilesListAsync(periodStart, periodEnd)).SkipLast(1).ToList();

            this.Logger.Info($"Video searching finished. Found {videos.Count} files");
            return videos;
        }

        private async Task<bool> DownloadRemoteVideoFileAsync(FileDTO file, CancellationToken token)
        {
            DateTime start = DateTime.Now;

            if (await this.Client.DownloadFileAsync(file, token))
            {
                file.DownloadStarted = start;
                var finish = DateTime.Now;
                var duration = (int?)(finish - start).TotalSeconds;
                file.DownloadDuration = duration;

                int? videoDutation = file.Duration;
                this.Logger.Info($"Duration {duration.FormatSeconds()}, avg speed {Utils.FormatBytes((long)(file.Size / duration))}/s");
                this.Logger.Info($"Video    {videoDutation.FormatSeconds()}, avg rate {Utils.FormatBytes((long)(file.Size / videoDutation))}/s");
                return true;
            }

            return false;
        }
    }
}

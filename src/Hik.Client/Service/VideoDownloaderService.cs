using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.Client.Helpers;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public class VideoDownloaderService : HikDownloaderServiceBase<MediaFileDTO>
    {
        public VideoDownloaderService(IDirectoryHelper directoryHelper, IClientFactory clientFactory)
            : base(directoryHelper, clientFactory)
        {
        }

        public override async Task<IReadOnlyCollection<MediaFileDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDTO> remoteFiles, CancellationToken token)
        {
            int j = 1;
            foreach (var video in remoteFiles)
            {
                ThrowIfCancellationRequested();
                logger.Debug($"{j++,2}/{remoteFiles.Count} : ");
                if (await DownloadRemoteVideoFileAsync(video, token))
                {
                    OnFileDownloaded(new FileDownloadedEventArgs(video));
                }
            }

            return remoteFiles;
        }

        public override async Task<IReadOnlyCollection<MediaFileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<MediaFileDTO> videos = (await this.Client.GetFilesListAsync(periodStart, periodEnd)).SkipLast(1).ToList();
            return videos;
        }

        private async Task<bool> DownloadRemoteVideoFileAsync(MediaFileDTO file, CancellationToken token)
        {
            DateTime start = DateTime.Now;

            if (await Client.DownloadFileAsync(file, token))
            {
                file.DownloadStarted = start;
                var finish = DateTime.Now;
                var duration = (int?)(finish - start).TotalSeconds;
                file.DownloadDuration = duration;

                int? videoDuration = file.Duration;
                logger.Debug($"Duration {duration.FormatSeconds()}, avg speed {((long)Utils.SafeDivision(file.Size, duration.Value)).FormatBytes()}/s");
                logger.Debug($"Video    {videoDuration.FormatSeconds()}, avg rate {((long)Utils.SafeDivision(file.Size, videoDuration.Value)).FormatBytes()}/s");
                return true;
            }

            return false;
        }
    }
}

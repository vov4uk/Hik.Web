using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Api.Data;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.Client.Helpers;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public class YiVideoDownloaderService : HikDownloaderServiceBase<VideoDTO>
    {
        public YiVideoDownloaderService(IDirectoryHelper directoryHelper, IHikClientFactory clientFactory, IMapper mapper)
            : base(directoryHelper, clientFactory, mapper)
        {
        }

        public override async Task<IReadOnlyCollection<VideoDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<IHikRemoteFile> remoteFiles, CancellationToken token)
        {
            int j = 1;
            var downloadedVideos = new List<VideoDTO>();
            foreach (IHikRemoteFile video in remoteFiles)
            {
                this.ThrowIfCancellationRequested();
                this.Logger.Info($"{j++,2}/{remoteFiles.Count} : ");

                var videoDownloadResult = await this.DownloadRemoteVideoFileAsync(video, token);
                if (videoDownloadResult != null)
                {
                    this.OnFileDownloaded(new FileDownloadedEventArgs(videoDownloadResult));
                    downloadedVideos.Add(videoDownloadResult);
                }
            }

            return downloadedVideos;
        }

        public override async Task<IReadOnlyCollection<IHikRemoteFile>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<RemoteVideoFile> videos = (await this.Client.FindVideosAsync(periodStart, periodEnd)).ToList();

            this.Logger.Info($"Video searching finished. Found {videos.Count} files");
            return videos;
        }

        private async Task<VideoDTO> DownloadRemoteVideoFileAsync(IHikRemoteFile file, CancellationToken token)
        {
            DateTime downloadStarted = DateTime.Now;
            if (await this.Client.DownloadFileAsync(file as RemoteVideoFile, token))
            {
                VideoDTO video = this.Mapper.Map<VideoDTO>(file);
                video.DownloadStartTime = downloadStarted;
                video.DownloadStopTime = DateTime.Now;
                TimeSpan duration = video.DownloadStopTime - downloadStarted;
                TimeSpan videoDutation = video.StopTime - video.StartTime;
                this.Logger.Info($"Duration {duration.ToString(DurationFormat)}, avg speed {Utils.FormatBytes((long)(file.Size / duration.TotalSeconds))}/s");
                this.Logger.Info($"Video    {videoDutation.ToString(DurationFormat)}, avg rate {Utils.FormatBytes((long)(file.Size / videoDutation.TotalSeconds))}/s");
                return video;
            }

            return default;
        }
    }
}

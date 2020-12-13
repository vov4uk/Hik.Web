using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using HikApi.Abstraction;
using HikApi.Data;
using HikConsole.Abstraction;
using HikConsole.DTO.Contracts;
using HikConsole.Events;
using HikConsole.Helpers;

namespace HikConsole.Service
{
    public class HikVideoDownloaderService : HikDownloaderServiceBase<VideoDTO>
    {
        public HikVideoDownloaderService(IDirectoryHelper directoryHelper, IHikClientFactory clientFactory, IMapper mapper)
            : base(directoryHelper, clientFactory, mapper)
        {
        }

        public int ProgressCheckPeriodMilliseconds { get; set; } = 5000;

        public override async Task<IReadOnlyCollection<VideoDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<IRemoteFile> remoteFiles)
        {
            int j = 1;
            var downloadedVideos = new List<VideoDTO>();
            foreach (IRemoteFile video in remoteFiles)
            {
                this.ThrowIfCancellationRequested();
                this.Logger.Info($"{j++,2}/{remoteFiles.Count} : ");
                var videoDownloadResult = await this.DownloadRemoteVideoFileAsync(video);
                if (videoDownloadResult != null)
                {
                    this.OnFileDownloaded(new FileDownloadedEventArgs(videoDownloadResult));
                    downloadedVideos.Add(videoDownloadResult);
                }
            }

            return downloadedVideos;
        }

        public override async Task<IReadOnlyCollection<IRemoteFile>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<RemoteVideoFile> videos = (await this.Client.FindVideosAsync(periodStart, periodEnd)).SkipLast(1).ToList();

            this.Logger.Info($"Video searching finished. Found {videos.Count} files");
            return videos;
        }

        private async Task<VideoDTO> DownloadRemoteVideoFileAsync(IRemoteFile file)
        {
            if (this.Client.StartVideoDownload(file))
            {
                DateTime downloadStarted = DateTime.Now;
                do
                {
                    await Task.Delay(this.ProgressCheckPeriodMilliseconds);
                    this.ThrowIfCancellationRequested();
                    this.Client.UpdateVideoProgress();
                }
                while (this.Client.IsDownloading);

                TimeSpan duration = DateTime.Now - downloadStarted;
                this.Logger.Info($"Download duration {duration.ToString(DurationFormat)}, avg speed {Utils.FormatBytes((long)(file.Size / duration.TotalSeconds))}/s");

                VideoDTO result = this.Mapper.Map<VideoDTO>(file);
                result.DownloadStartTime = downloadStarted;
                result.DownloadStopTime = DateTime.Now;
                return result;
            }

            return default;
        }
    }
}

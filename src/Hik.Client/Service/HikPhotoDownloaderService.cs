using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;

namespace Hik.Client.Service
{
    public class HikPhotoDownloaderService : VideoDownloaderService, IHikPhotoDownloaderService
    {
        public HikPhotoDownloaderService(IDirectoryHelper directoryHelper, IClientFactory clientFactory)
            : base(directoryHelper, clientFactory)
        {
        }

        public override async Task<IReadOnlyCollection<MediaFileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<MediaFileDTO> photos = (await this.Client.GetFilesListAsync(periodStart, periodEnd)).ToList();

            this.logger.Info($"Found {photos.Count} photos");
            return photos;
        }

        public override async Task<IReadOnlyCollection<MediaFileDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDTO> remoteFiles, CancellationToken token)
        {
            foreach (var photo in remoteFiles)
            {
                DateTime start = DateTime.Now;
                this.ThrowIfCancellationRequested();
                bool isDownloaded = await this.Client.DownloadFileAsync(photo, token);
                DateTime finish = DateTime.Now;

                if (isDownloaded)
                {
                    photo.DownloadStarted = start;
                    photo.DownloadDuration = (int)(finish - start).TotalSeconds;
                    this.OnFileDownloaded(new FileDownloadedEventArgs(photo));
                }
            }

            return remoteFiles;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Abstraction.Services;
using Hik.Client.Events;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;

namespace Hik.Client.Service
{
    public class HikPhotoDownloaderService : VideoDownloaderService, IHikPhotoDownloaderService
    {
        public HikPhotoDownloaderService(IDirectoryHelper directoryHelper, IClientFactory clientFactory, ILogger logger)
            : base(directoryHelper, clientFactory, logger)
        {
        }

        public override async Task<IReadOnlyCollection<MediaFileDto>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<MediaFileDto> photos = (await this.Client.GetFilesListAsync(periodStart, periodEnd)).ToList();

            this.logger.LogInformation("Found {count} photos", photos.Count);
            return photos;
        }

        public override async Task<IReadOnlyCollection<MediaFileDto>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDto> remoteFiles, CancellationToken token)
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
                else
                {
                    photo.Path = "FailedToDownload";
                }
            }

            return remoteFiles;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Client.Abstraction;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public class HikPhotoDownloaderService : HikDownloaderServiceBase<MediaFileDTO>
    {
        public HikPhotoDownloaderService(IDirectoryHelper directoryHelper, IClientFactory clientFactory, IMapper mapper)
            : base(directoryHelper, clientFactory, mapper)
        {
        }

        public override async Task<IReadOnlyCollection<MediaFileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<MediaFileDTO> photos = (await this.Client.GetFilesListAsync(periodStart, periodEnd)).ToList();
            var resultCountString = photos.Count.ToString();

            this.logger.Info("Photos searching finished.");
            this.logger.Info($"Found {resultCountString} photos");
            return photos;
        }

        public override async Task<IReadOnlyCollection<MediaFileDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDTO> remoteFiles, CancellationToken token = default)
        {
            var photoDownloadResults = new Dictionary<bool, int>
            {
                { false, 0 },
                { true, 0 },
            };

            int j = 0;

            foreach (var photo in remoteFiles)
            {
                DateTime start = DateTime.Now;
                this.ThrowIfCancellationRequested();
                bool isDownloaded = await this.Client.DownloadFileAsync(photo, token);
                DateTime finish = DateTime.Now;
                photoDownloadResults[isDownloaded]++;
                j++;

                // TODO report downloading progress via event
                if (isDownloaded)
                {
                    photo.DownloadStarted = start;
                    photo.DownloadDuration = (int)(finish - start).TotalSeconds;
                }
            }

            this.logger.Info($"Exist {photoDownloadResults[false]} photos");
            this.logger.Info($"Downloaded {photoDownloadResults[true]} photos");
            return remoteFiles;
        }
    }
}

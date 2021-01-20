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
    public class HikPhotoDownloaderService : HikDownloaderServiceBase<FileDTO>
    {
        public HikPhotoDownloaderService(IDirectoryHelper directoryHelper, IClientFactory clientFactory, IMapper mapper)
            : base(directoryHelper, clientFactory, mapper)
        {
        }

        public override async Task<IReadOnlyCollection<FileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<FileDTO> photos = (await this.Client.GetFilesListAsync(periodStart, periodEnd)).ToList();
            var resultCountString = photos.Count.ToString();

            this.Logger.Info("Photos searching finished.");
            this.Logger.Info($"Found {resultCountString} photos");
            return photos;
        }

        public override async Task<IReadOnlyCollection<FileDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<FileDTO> remoteFiles, CancellationToken token = default)
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
                    photo.DownloadStartTime = start;
                    photo.DownloadStopTime = finish;
                }
            }

            this.Logger.Info($"Exist {photoDownloadResults[false]} photos");
            this.Logger.Info($"Downloaded {photoDownloadResults[true]} photos");
            return remoteFiles;
        }
    }
}

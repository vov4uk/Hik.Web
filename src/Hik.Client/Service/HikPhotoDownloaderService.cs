using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hik.Api.Abstraction;
using Hik.Api.Data;
using Hik.Client.Abstraction;
using Hik.Client.Service;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public class HikPhotoDownloaderService : HikDownloaderServiceBase<PhotoDTO>
    {
        public HikPhotoDownloaderService(IDirectoryHelper directoryHelper, IHikClientFactory clientFactory, IMapper mapper)
            : base(directoryHelper, clientFactory, mapper)
        {
        }

        public override async Task<IReadOnlyCollection<IRemoteFile>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            List<RemotePhotoFile> photos = (await this.Client.FindPhotosAsync(periodStart, periodEnd)).ToList();
            var resultCountString = photos.Count.ToString();

            this.Logger.Info("Photos searching finished.");
            this.Logger.Info($"Found {resultCountString} photos");
            return photos;
        }

        public override async Task<IReadOnlyCollection<PhotoDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<IRemoteFile> remoteFiles)
        {
            var photoDownloadResults = new Dictionary<bool, int>
            {
                { false, 0 },
                { true, 0 },
            };

            int j = 0;
            var photosFromClient = new List<PhotoDTO>();

            return await Task.Run(() =>
            {
                foreach (RemotePhotoFile photo in remoteFiles)
                {
                    DateTime start = DateTime.Now;
                    this.ThrowIfCancellationRequested();
                    bool isDownloaded = this.Client.PhotoDownload(photo);
                    DateTime finish = DateTime.Now;
                    photoDownloadResults[isDownloaded]++;
                    j++;

                    // TODO report downloading progress via event
                    if (isDownloaded)
                    {
                        var photoDto = this.Mapper.Map<PhotoDTO>(photo);
                        photoDto.DownloadStartTime = start;
                        photoDto.DownloadStopTime = finish;

                        photosFromClient.Add(photoDto);
                    }
                }

                this.Logger.Info($"Exist {photoDownloadResults[false]} photos");
                this.Logger.Info($"Downloaded {photoDownloadResults[true]} photos");
                return photosFromClient;
            });
        }
    }
}

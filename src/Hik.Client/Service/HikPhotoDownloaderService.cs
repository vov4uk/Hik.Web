using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public class HikPhotoDownloaderService : VideoDownloaderService
    {
        public HikPhotoDownloaderService(IDirectoryHelper directoryHelper, IClientFactory clientFactory)
            : base(directoryHelper, clientFactory)
        {
        }

        protected override Task<IReadOnlyCollection<MediaFileDTO>> GetRemoteFilesList(DateTime periodStart, DateTime periodEnd)
        {
            return Client.GetFilesListAsync(periodStart, periodEnd);
        }

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> DownloadFilesFromClientAsync(IReadOnlyCollection<MediaFileDTO> remoteFiles, CancellationToken token)
        {
            foreach (var photo in remoteFiles)
            {
                DateTime start = DateTime.Now;
                ThrowIfCancellationRequested();
                bool isDownloaded = await Client.DownloadFileAsync(photo, token);
                DateTime finish = DateTime.Now;

                if (isDownloaded)
                {
                    photo.DownloadStarted = start;
                    photo.DownloadDuration = (int)(finish - start).TotalSeconds;
                    OnFileDownloaded(new FileDownloadedEventArgs(photo));
                }
            }

            return remoteFiles;
        }
    }
}

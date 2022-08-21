using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hik.DTO.Contracts;

namespace Hik.Client.Abstraction
{
    public interface IDownloaderClient : IClientBase
    {
        Task<bool> DownloadFileAsync(MediaFileDto remoteFile, CancellationToken token);

        Task<IReadOnlyCollection<MediaFileDto>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd);
    }
}

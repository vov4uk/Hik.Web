using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hik.DTO.Contracts;

namespace Hik.Client.Abstraction
{
    public interface IClient : IDisposable
    {
        bool IsDownloading { get; }

        void InitializeClient();

        bool Login();

        Task<bool> DownloadFileAsync(MediaFileDTO remoteFile, CancellationToken token);

        void ForceExit();

        Task<IList<MediaFileDTO>> GetFilesListAsync(DateTime periodStart, DateTime periodEnd);
    }
}

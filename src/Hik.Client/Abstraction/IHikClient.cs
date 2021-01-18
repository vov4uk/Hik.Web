using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hik.Api.Abstraction;
using Hik.Api.Data;

namespace Hik.Client.Abstraction
{
    public interface IHikClient : IDisposable
    {
        bool IsDownloading { get; }

        void InitializeClient();

        bool Login();

        HdInfo CheckHardDriveStatus();

        bool PhotoDownload(RemotePhotoFile remoteFile);

        bool StartVideoDownload(IHikRemoteFile remoteFile);

        Task<bool> DownloadFileAsync(RemoteVideoFile remoteFile, CancellationToken token);

        void StopVideoDownload();

        void UpdateVideoProgress();

        void ForceExit();

        Task<IList<RemoteVideoFile>> FindVideosAsync(DateTime periodStart, DateTime periodEnd);

        Task<IList<RemotePhotoFile>> FindPhotosAsync(DateTime periodStart, DateTime periodEnd);
    }
}

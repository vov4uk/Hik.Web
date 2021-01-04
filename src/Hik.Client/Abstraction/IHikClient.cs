namespace Hik.Client.Abstraction
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Hik.Api.Abstraction;
    using Hik.Api.Data;

    public interface IHikClient : IDisposable
    {
        bool IsDownloading { get; }

        void InitializeClient();

        bool Login();

        HdInfo CheckHardDriveStatus();

        bool PhotoDownload(RemotePhotoFile remoteFile);

        bool StartVideoDownload(IRemoteFile remoteFile);

        void StopVideoDownload();

        void UpdateVideoProgress();

        void ForceExit();

        Task<IList<RemoteVideoFile>> FindVideosAsync(DateTime periodStart, DateTime periodEnd);

        Task<IList<RemotePhotoFile>> FindPhotosAsync(DateTime periodStart, DateTime periodEnd);
    }
}

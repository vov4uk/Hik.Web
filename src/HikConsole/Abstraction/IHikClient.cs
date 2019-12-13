using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HikApi.Data;

namespace HikConsole.Abstraction
{
    public interface IHikClient : IDisposable
    {
        bool IsDownloading { get; }

        void InitializeClient();

        bool Login();

        void CheckHardDriveStatus();

        bool PhotoDownload(RemotePhotoFile remoteFile);

        bool StartVideoDownload(RemoteVideoFile remoteFile);

        void StopVideoDownload();

        void UpdateVideoProgress();

        void ForceExit();

        Task<IList<RemoteVideoFile>> FindVideosAsync(DateTime periodStart, DateTime periodEnd);

        Task<IList<RemotePhotoFile>> FindPhotosAsync(DateTime periodStart, DateTime periodEnd);
    }
}

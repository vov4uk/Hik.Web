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

        bool StartDownload(RemoteVideoFile remoteFile);

        void StopDownload();

        void UpdateProgress();

        void ForceExit();

        Task<IList<RemoteVideoFile>> FindAsync(DateTime periodStart, DateTime periodEnd);
    }
}

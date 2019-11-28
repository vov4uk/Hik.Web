using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HikApi.Data;

namespace HikConsole.Abstraction
{
    public interface IHikClient
    {
        bool IsDownloading { get; }

        void InitializeClient();

        bool Login();

        bool StartDownload(RemoteVideoFile file);

        void StopDownload();

        void UpdateProgress();

        void Logout();

        void ForceExit();

        Task<IList<RemoteVideoFile>> FindAsync(DateTime periodStart, DateTime periodEnd);
    }
}

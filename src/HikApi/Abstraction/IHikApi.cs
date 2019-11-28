using HikApi.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HikApi.Abstraction
{
    public interface IHikApi
    {
        bool Initialize();

        bool SetupLogs(int logingEnable, string logDirectory, bool autoDelete);

        LoginResult Login(string ipAdress, int port, string userName, string password);

        Task<IList<RemoteVideoFile>> SearchVideoFilesAsync(DateTime periodStart, DateTime periodEnd, int userId, int channel);

        int StartDownloadFile(int userId, string sourceFileName, string destenationFilePath);

        void StopDownloadFile(int fileHandle);

        int GetDownloadPosition(int fileHandle);

        void Logout(int userId);

        void Cleanup();

        void CloseSearch(int fileHandle);
    }
}

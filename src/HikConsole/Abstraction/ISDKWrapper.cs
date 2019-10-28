using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HikConsole.Data;

namespace HikConsole.Abstraction
{
    public interface ISDKWrapper
    {
        bool Initialize();

        bool SetupSDKLogs(int bLogEnable, string strLogDir, bool bAutoDel);

        int Login(string ipAdress, int port, string userName, string password, ref DeviceInfo deviceInfo);

        Task<IList<FindResult>> Find(DateTime periodStart, DateTime periodEnd, int userid, int channel);

        int StartDownloadFile(int userId, string fileName, string savedFileName);

        void StopDownoloadFile(int fileHandle);

        int GetDownloadPos(int fileHandle);

        void Logout(int userId);

        void Cleanup();

        void CloseSearch(int fileHandle);
    }
}

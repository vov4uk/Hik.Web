using HikApi.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HikApi.Abstraction
{
    public interface IHikApi
    {
        bool Initialize();

        /// <summary>Setups the logs.</summary>
        /// <param name="logLevel">Log level. 0- close log(default), 1- output ERROR log only, 2- output ERROR and DEBUG log, 3- output all log, including ERROR, DEBUG and INFO log</param>
        /// <param name="logDirectory">The log directory. Log file saving path, if set to NULL, the default path for Windows is "C:\\SdkLog\\", and the default path for Linux is ""/home/sdklog/"</param>
        /// <param name="autoDelete">Whether to delete the files which exceed the number limit. Default: TRUE.</param>
        /// <returns></returns>
        bool SetupLogs(int logLevel, string logDirectory, bool autoDelete);

        Session Login(string ipAdress, int port, string userName, string password);

        Task<IList<RemoteVideoFile>> SearchVideoFilesAsync(DateTime periodStart, DateTime periodEnd, int userId, int channel);

        int StartDownloadFile(int userId, string sourceFileName, string destenationFilePath);

        void StopDownloadFile(int fileHandle);

        int GetDownloadPosition(int fileHandle);

        void Logout(int userId);

        void Cleanup();

        void CloseSearch(int fileHandle);
    }
}

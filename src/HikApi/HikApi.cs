using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using HikApi.Abstraction;
using HikApi.Data;
using HikApi.Struct;

namespace HikApi
{
    [ExcludeFromCodeCoverage]
    public class HikApi : IHikApi
    {
        private const string DllPath = @"SDK\HCNetSDK";

        public bool Initialize()
        {
            return this.InvokeSDK(() => HikApi.NET_DVR_Init());
        }

        public bool SetupLogs(int logLevel, string logDirectory, bool autoDelete)
        {
            return this.InvokeSDK(() => HikApi.NET_DVR_SetLogToFile(logLevel, logDirectory, autoDelete));
        }

        public Session Login(string ipAdress, int port, string userName, string password)
        {
            NET_DVR_DEVICEINFO_V30 deviceInfo = default(NET_DVR_DEVICEINFO_V30);
            int userId = this.InvokeSDK(() => HikApi.NET_DVR_Login_V30(ipAdress, port, userName, password, ref deviceInfo));
            return new Session(userId, deviceInfo.byChanNum, deviceInfo.byStartChan);
        }

        public async Task<IList<RemoteVideoFile>> FindVideoFilesAsync(DateTime periodStart, DateTime periodEnd, int userId, int channel)
        {
            NET_DVR_FILECOND_V40 findCondition = this.PrepareFindCondition(periodStart, periodEnd, channel);

            int findId = this.StartFind(userId, findCondition);
            IEnumerable<RemoteVideoFile> results = await this.GetFindResults(findId);

            this.FindClose(findId);
            return results.SkipLast(1).ToList(); // skip last, because this file still recording
        }

        public int StartDownloadFile(int userId, string sourceFileName, string destenationFilePath)
        {
            int downloadHandle = this.InvokeSDK(() => HikApi.NET_DVR_GetFileByName(userId, sourceFileName, destenationFilePath));

            uint iOutValue = 0;
            this.InvokeSDK(() => HikApi.NET_DVR_PlayBackControl_V40(downloadHandle, HikConst.NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue));
            return downloadHandle;
        }

        public void StopDownloadFile(int fileHandle)
        {
            this.InvokeSDK(() => HikApi.NET_DVR_StopGetFile(fileHandle));
        }

        public int GetDownloadPosition(int fileHandle)
        {
            return this.InvokeSDK(() => HikApi.NET_DVR_GetDownloadPos(fileHandle));
        }

        public void FindClose(int fileHandle)
        {
            this.InvokeSDK(() => HikApi.NET_DVR_FindClose_V30(fileHandle));
        }

        public void Cleanup()
        {
            this.InvokeSDK(() => HikApi.NET_DVR_Cleanup());
        }

        public void Logout(int userId)
        {
            this.InvokeSDK(() => HikApi.NET_DVR_Logout(userId));
        }

        private NET_DVR_FILECOND_V40 PrepareFindCondition(DateTime periodStart, DateTime periodEnd, int channel)
        {
            NET_DVR_FILECOND_V40 findConditions = new NET_DVR_FILECOND_V40
            {
                lChannel = channel,
                dwFileType = 0xff, // all
                dwIsLocked = 0xff, // all, locked and unlocked
                struStartTime = new NET_DVR_TIME(periodStart),
                struStopTime = new NET_DVR_TIME(periodEnd),
            };
            return findConditions;
        }

        private int StartFind(int userId, NET_DVR_FILECOND_V40 findConditions)
        {
            return this.InvokeSDK(() => HikApi.NET_DVR_FindFile_V40(userId, ref findConditions));
        }

        private async Task<IEnumerable<RemoteVideoFile>> GetFindResults(int findId)
        {
            var results = new List<RemoteVideoFile>();
            while (true)
            {
                NET_DVR_FINDDATA_V30 findData = default(NET_DVR_FINDDATA_V30);
                int findStatus = HikApi.NET_DVR_FindNextFile_V30(findId, ref findData);

                if (findStatus == HikConst.NET_DVR_ISFINDING)
                {
                    await Task.Delay(500);
                }
                else if (findStatus == HikConst.NET_DVR_FILE_SUCCESS)
                {
                   results.Add( new RemoteVideoFile(findData));
                }
                else
                {
                    break;
                }
            }

            return results;
        }
            private HikException CreateException(string method)
        {
            uint lastErrorCode = HikApi.NET_DVR_GetLastError();
            return new HikException(method, lastErrorCode);
        }

        private int InvokeSDK(Expression<Func<int>> func)
        {
            int result = func.Compile().Invoke();
            if (result < 0)
            {
                throw this.CreateException(func.ToString());
            }

            return result;
        }

        private bool InvokeSDK(Expression<Func<bool>> func)
        {
            bool result = func.Compile().Invoke();
            if (!result)
            {
                throw this.CreateException(func.ToString());
            }

            return result;
        }


        [DllImport(DllPath)]
        private static extern int NET_DVR_FindFile_V40(int lUserID, ref Struct.NET_DVR_FILECOND_V40 pFindCond);

        [DllImport(DllPath)]
        private static extern uint NET_DVR_GetLastError();

        [DllImport(DllPath)]
        private static extern int NET_DVR_FindNextFile_V30(int lFindHandle, ref Struct.NET_DVR_FINDDATA_V30 lpFindData);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_StopGetFile(int lFileHandle);

        [DllImport(DllPath)]
        private static extern int NET_DVR_GetDownloadPos(int lFileHandle);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_PlayBackControl_V40(int lPlayHandle, uint dwControlCode, IntPtr lpInBuffer, uint dwInValue, IntPtr lpOutBuffer, ref uint lPOutValue);

        [DllImport(DllPath)]
        private static extern int NET_DVR_GetFileByName(int lUserID, string sDVRFileName, string sSavedFileName);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_Init();

        [DllImport(DllPath)]
        private static extern bool NET_DVR_SetLogToFile(int bLogEnable, string strLogDir, bool bAutoDel);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_Logout(int iUserID);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_FindClose_V30(int lFindHandle);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_Cleanup();

        [DllImport(DllPath)]
        private static extern int NET_DVR_Login_V30(string sDVRIP, int wDVRPort, string sUserName, string sPassword, ref Struct.NET_DVR_DEVICEINFO_V30 lpDeviceInfo);
    }
}
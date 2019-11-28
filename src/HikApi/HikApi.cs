using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
            return this.InvokeApi(HikApi.NET_DVR_Init, "NET_DVR_Init");
        }

        public bool SetupLogs(int logingEnable, string logDirectory, bool autoDelete)
        {
            return this.InvokeApi(() => HikApi.NET_DVR_SetLogToFile(logingEnable, logDirectory, autoDelete), "NET_DVR_SetLogToFile");
        }

        public LoginResult Login(string ipAdress, int port, string userName, string password)
        {
            NET_DVR_DEVICEINFO_V30 deviceInfo = default(NET_DVR_DEVICEINFO_V30);
            int userId = this.InvokeApi(() => HikApi.NET_DVR_Login_V30(ipAdress, port, userName, password, ref deviceInfo), "NET_DVR_Login_V30");
            return new LoginResult(userId, deviceInfo.byChanNum, deviceInfo.byStartChan);
        }

        public async Task<IList<RemoteVideoFile>> SearchVideoFilesAsync(DateTime periodStart, DateTime periodEnd, int userId, int channel)
        {
            var results = new List<RemoteVideoFile>();

            NET_DVR_FILECOND_V40 searchCondition = this.PrepareSearchCondition(periodStart, periodEnd, channel);

            int searchId = this.StartSearch(userId, searchCondition);

            while (true)
            {
                NET_DVR_FINDDATA_V30 findData = default(NET_DVR_FINDDATA_V30);
                int findStatus = HikApi.NET_DVR_FindNextFile_V30(searchId, ref findData);

                if (findStatus == HikConst.NET_DVR_ISFINDING)
                {
                    await Task.Delay(500);
                }
                else if (findStatus == HikConst.NET_DVR_FILE_SUCCESS)
                {
                    results.Add(new RemoteVideoFile(findData));
                }
                else
                {
                    break;
                }
            }

            this.CloseSearch(searchId);
            return results.SkipLast(1).ToList(); // skip last, because this file still recording
        }

        public int StartDownloadFile(int userId, string sourceFileName, string destenationFilePath)
        {
            int downloadHandle = this.InvokeApi(() => HikApi.NET_DVR_GetFileByName(userId, sourceFileName, destenationFilePath), "NET_DVR_GetFileByName");

            uint iOutValue = 0;
            this.InvokeApi(() => HikApi.NET_DVR_PlayBackControl_V40(downloadHandle, HikConst.NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue), "NET_DVR_PlayBackControl_V40");
            return downloadHandle;
        }

        public void StopDownloadFile(int fileHandle)
        {
            this.InvokeApi(() => HikApi.NET_DVR_StopGetFile(fileHandle), "NET_DVR_StopGetFile");
        }

        public int GetDownloadPosition(int fileHandle)
        {
            return this.InvokeApi(() => HikApi.NET_DVR_GetDownloadPos(fileHandle), "NET_DVR_GetDownloadPos");
        }

        public void CloseSearch(int fileHandle)
        {
            this.InvokeApi(() => HikApi.NET_DVR_FindClose_V30(fileHandle), "NET_DVR_FindClose_V30");
        }

        public void Cleanup()
        {
            this.InvokeApi(HikApi.NET_DVR_Cleanup, "NET_DVR_Cleanup");
        }

        public void Logout(int userId)
        {
            this.InvokeApi(() => HikApi.NET_DVR_Logout(userId), "NET_DVR_Logout");
        }

        private NET_DVR_FILECOND_V40 PrepareSearchCondition(DateTime periodStart, DateTime periodEnd, int channel)
        {
            NET_DVR_FILECOND_V40 findConditions = new NET_DVR_FILECOND_V40
            {
                lChannel = channel,
                dwFileType = 0xff,
                dwIsLocked = 0xff,
                struStartTime = new NET_DVR_TIME(periodStart),
                struStopTime = new NET_DVR_TIME(periodEnd),
            };
            return findConditions;
        }

        private int StartSearch(int userId, NET_DVR_FILECOND_V40 findConditions)
        {
            return this.InvokeApi(() => HikApi.NET_DVR_FindFile_V40(userId, ref findConditions), "NET_DVR_FindFile_V40");
        }

        private HikException CreateException(string method)
        {
            uint lastErrorCode = HikApi.NET_DVR_GetLastError();
            return new HikException(method, lastErrorCode);
        }

        private int InvokeApi(Func<int> func, string methodName)
        {
            int result = func.Invoke();
            if (result < 0)
            {
                throw this.CreateException(methodName);
            }

            return result;
        }

        private bool InvokeApi(Func<bool> func, string methodName)
        {
            bool result = func.Invoke();
            if (!result)
            {
                throw this.CreateException(methodName);
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
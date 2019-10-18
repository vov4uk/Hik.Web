using HikConsole.Abstraction;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HikConsole.Data;
using HikConsole.Helpers;
using System.Linq;

namespace HikConsole.SDK
{
    public class SDKWrapper : ISDKWrapper
    {
        public bool Initialize()
        {
            if (!NetSDK.NET_DVR_Init())
            {
                throw new Exception(nameof(NetSDK.NET_DVR_Init));
            }
            return true;
        }

        public bool SetupSDKLogs(int bLogEnable, string strLogDir, bool bAutoDel)
        {
            if (!NetSDK.NET_DVR_SetLogToFile(bLogEnable, strLogDir, bAutoDel))
            {
                throw new Exception(nameof(NetSDK.NET_DVR_SetLogToFile));
            }
            return true;
        }

        public int Login(string ipAdress, int port, string userName, string password, ref DeviceInfo deviceInfo)
        {
            NetSDK.NET_DVR_DEVICEINFO_V30 lpDeviceInfo = default;
            int _userId = NetSDK.NET_DVR_Login_V30(ipAdress, port, userName, password, ref lpDeviceInfo);
            if (_userId < 0)
            {
                throw new Exception(nameof(NetSDK.NET_DVR_Login_V30));
            }
            deviceInfo = new DeviceInfo() { ChannelNumber = lpDeviceInfo.byChanNum, StartChannel = lpDeviceInfo.byStartChan };
            return _userId;
        }

        public async Task<IList<FindResult>> Find(DateTime periodStart, DateTime periodEnd, int userid, int channel)
        {
            var results = new List<FindResult>();

            NetSDK.NET_DVR_FILECOND_V40 findCond = new NetSDK.NET_DVR_FILECOND_V40
            {
                lChannel = channel,
                dwFileType = 0xff,
                dwIsLocked = 0xff,
                struStartTime = new NetSDK.NET_DVR_TIME(periodStart),
                struStopTime = new NetSDK.NET_DVR_TIME(periodEnd)
            };

            var findHandle = NetSDK.NET_DVR_FindFile_V40(userid, ref findCond);

            if (findHandle < 0)
            {
                throw new Exception(nameof(NetSDK.NET_DVR_FindFile_V40));
            }

            while (true)
            {
                NetSDK.NET_DVR_FINDDATA_V30 fileData = new NetSDK.NET_DVR_FINDDATA_V30();
                int findResult = NetSDK.NET_DVR_FindNextFile_V30(findHandle, ref fileData);

                if (findResult == NetSDK.NET_DVR_ISFINDING)
                {
                    await Task.Delay(500);
                }
                else if (findResult == NetSDK.NET_DVR_FILE_SUCCESS)
                {
                    results.Add(new FindResult()
                    {
                        FileName = fileData.sFileName,
                        StartTime = fileData.struStartTime.ToDateTime(),
                        StopTime = fileData.struStopTime.ToDateTime(),
                        FileSize = fileData.dwFileSize
                    });

                }
                else if (findResult == NetSDK.NET_DVR_FILE_NOFIND || findResult == NetSDK.NET_DVR_NOMOREFILE)
                {
                    break;
                }
                else
                {
                    break;
                }
            }

            return results.SkipLast(1).ToList();
        }

        public int GetFileByName(int userID, string fileName, string savedFileName)
        {
            var downloadHandle = NetSDK.NET_DVR_GetFileByName(userID, fileName, savedFileName);
            if (downloadHandle < 0)
            {
                throw new Exception(nameof(NetSDK.NET_DVR_GetFileByName));
            }

            uint iOutValue = 0;
            if (!NetSDK.NET_DVR_PlayBackControl_V40(downloadHandle, NetSDK.NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
            {
                throw new Exception(nameof(NetSDK.NET_DVR_PlayBackControl_V40));
            }

            return downloadHandle;
        }

        public void StopDownoloadFile(int fileHandle)
        {
            if (!NetSDK.NET_DVR_StopGetFile(fileHandle))
            {
                throw new Exception(nameof(NetSDK.NET_DVR_StopGetFile));
            }
        }

        public int GetDownloadPos(int fileHandle)
        {
            return NetSDK.NET_DVR_GetDownloadPos(fileHandle);
        }

        public void Logout(int userId)
        {
            if (!NetSDK.NET_DVR_Logout(userId))
            {
                throw new Exception(nameof(NetSDK.NET_DVR_Logout));
            }
        }

        public static uint GetLastError()
        {
            return NetSDK.NET_DVR_GetLastError();
        }
    }
}

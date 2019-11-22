using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HikConsole.Abstraction;
using HikConsole.Data;

namespace HikConsole.SDK
{
    [ExcludeFromCodeCoverage]
    public class SDKWrapper : ISDKWrapper
    {
        public bool Initialize()
        {
            if (!NetSDK.NET_DVR_Init())
            {
                throw this.CreateException(nameof(NetSDK.NET_DVR_Init));
            }

            return true;
        }

        public bool SetupSDKLogs(int bLogEnable, string strLogDir, bool bAutoDel)
        {
            if (!NetSDK.NET_DVR_SetLogToFile(bLogEnable, strLogDir, bAutoDel))
            {
                throw this.CreateException(nameof(NetSDK.NET_DVR_SetLogToFile));
            }

            return true;
        }

        public int Login(string ipAdress, int port, string userName, string password, ref DeviceInfo deviceInfo)
        {
            NetSDK.NET_DVR_DEVICEINFO_V30 lpDeviceInfo = default(NetSDK.NET_DVR_DEVICEINFO_V30);
            int userId = NetSDK.NET_DVR_Login_V30(ipAdress, port, userName, password, ref lpDeviceInfo);
            if (userId < 0)
            {
                throw this.CreateException(nameof(NetSDK.NET_DVR_Login_V30));
            }

            deviceInfo = new DeviceInfo() { ChannelNumber = lpDeviceInfo.byChanNum, StartChannel = lpDeviceInfo.byStartChan };
            return userId;
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
                struStopTime = new NetSDK.NET_DVR_TIME(periodEnd),
            };

            var findHandle = NetSDK.NET_DVR_FindFile_V40(userid, ref findCond);

            if (findHandle < 0)
            {
                throw this.CreateException(nameof(NetSDK.NET_DVR_FindFile_V40), "find files failed");
            }

            while (true)
            {
                NetSDK.NET_DVR_FINDDATA_V30 fileData = default(NetSDK.NET_DVR_FINDDATA_V30);
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
                        FileSize = fileData.dwFileSize,
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

            this.CloseSearch(findHandle);
            return results.SkipLast(1).ToList();
        }

        public int StartDownloadFile(int userId, string fileName, string savedFileName)
        {
            var downloadHandle = NetSDK.NET_DVR_GetFileByName(userId, fileName, savedFileName);
            if (downloadHandle < 0)
            {
                throw this.CreateException(nameof(NetSDK.NET_DVR_GetFileByName));
            }

            uint iOutValue = 0;
            if (!NetSDK.NET_DVR_PlayBackControl_V40(downloadHandle, NetSDK.NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue))
            {
                throw this.CreateException(nameof(NetSDK.NET_DVR_PlayBackControl_V40));
            }

            return downloadHandle;
        }

        public void StopDownoloadFile(int fileHandle)
        {
            if (!NetSDK.NET_DVR_StopGetFile(fileHandle))
            {
                throw this.CreateException(nameof(NetSDK.NET_DVR_StopGetFile));
            }
        }

        public int GetDownloadPos(int fileHandle)
        {
            return NetSDK.NET_DVR_GetDownloadPos(fileHandle);
        }

        public void CloseSearch(int fileHandle)
        {
            if (fileHandle > 0 && NetSDK.NET_DVR_FindClose_V30(fileHandle))
            {
                throw this.CreateException(nameof(NetSDK.NET_DVR_FindClose_V30));
            }
        }

        public void Cleanup()
        {
            NetSDK.NET_DVR_Cleanup();
        }

        public void Logout(int userId)
        {
            if (!NetSDK.NET_DVR_Logout(userId))
            {
                throw this.CreateException(nameof(NetSDK.NET_DVR_Logout));
            }
        }

        private SdkException CreateException(string method, string message = null)
        {
            uint lastErrorCode = NetSDK.NET_DVR_GetLastError();

            Error last = (Error)lastErrorCode;
            string msg = string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                msg = $" : {message}";
            }

            return new SdkException($"{method} failed, error code = {lastErrorCode.ToString()}{Environment.NewLine}{this.GetEnumDescription(last)}{Environment.NewLine}{msg}");
        }

        private string GetEnumDescription(Error value)
        {
            string val = value.ToString();
            FieldInfo fi = value.GetType().GetField(val);

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return val;
        }
    }
}

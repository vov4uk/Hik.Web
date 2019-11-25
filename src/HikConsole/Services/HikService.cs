using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HikApi;
using HikApi.Struct;
using HikConsole.Abstraction;
using HikConsole.Data;

namespace HikConsole.Services
{
    [ExcludeFromCodeCoverage]
    public class HikService : IHikService
    {
        public bool Initialize()
        {
            return this.InvokeApi(Api.NET_DVR_Init);
        }

        public bool SetupLogs(int logingEnable, string logDirectory, bool autoDelete)
        {
            return this.InvokeApi(() => Api.NET_DVR_SetLogToFile(logingEnable, logDirectory, autoDelete));
        }

        public LoginResult Login(string ipAdress, int port, string userName, string password)
        {
            NET_DVR_DEVICEINFO_V30 deviceInfo = default(NET_DVR_DEVICEINFO_V30);
            int userId = this.InvokeApi(() => Api.NET_DVR_Login_V30(ipAdress, port, userName, password, ref deviceInfo));
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
                int findStatus = Api.NET_DVR_FindNextFile_V30(searchId, ref findData);

                if (findStatus == HikConst.NET_DVR_ISFINDING)
                {
                    await Task.Delay(500);
                }
                else if (findStatus == HikConst.NET_DVR_FILE_SUCCESS)
                {
                    results.Add(new RemoteVideoFile(findData));
                }
                else if (findStatus == HikConst.NET_DVR_FILE_NOFIND || findStatus == HikConst.NET_DVR_NOMOREFILE)
                {
                    break;
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
            int downloadHandle = this.InvokeApi(() => Api.NET_DVR_GetFileByName(userId, sourceFileName, destenationFilePath));
            uint iOutValue = 0;

            this.InvokeApi(() => Api.NET_DVR_PlayBackControl_V40(downloadHandle, HikConst.NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue));
            return downloadHandle;
        }

        public void StopDownloadFile(int fileHandle)
        {
            this.InvokeApi(() => Api.NET_DVR_StopGetFile(fileHandle));
        }

        public int GetDownloadPosition(int fileHandle)
        {
            return Api.NET_DVR_GetDownloadPos(fileHandle);
        }

        public void CloseSearch(int fileHandle)
        {
            this.InvokeApi(() => Api.NET_DVR_FindClose_V30(fileHandle));
        }

        public void Cleanup()
        {
            this.InvokeApi(Api.NET_DVR_Cleanup);
        }

        public void Logout(int userId)
        {
            this.InvokeApi(() => Api.NET_DVR_Logout(userId));
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
            return this.InvokeApi(() => Api.NET_DVR_FindFile_V40(userId, ref findConditions));
        }

        private HikException CreateException(string method, string message = null)
        {
            uint lastErrorCode = Api.NET_DVR_GetLastError();

            HikError last = (HikError)lastErrorCode;
            string msg = string.Empty;
            if (!string.IsNullOrEmpty(message))
            {
                msg = $" : {message}";
            }

            return new HikException($"{method} failed, error code = {lastErrorCode.ToString()}{Environment.NewLine}{this.GetEnumDescription(last)}{Environment.NewLine}{msg}");
        }

        private string GetEnumDescription(HikError value)
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

        private int InvokeApi(Func<int> func, string error = null)
        {
            int result = func.Invoke();
            if (result < 0)
            {
                throw this.CreateException(nameof(func), error);
            }

            return result;
        }

        private bool InvokeApi(Func<bool> func, string error = null)
        {
            bool result = func.Invoke();
            if (!result)
            {
                throw this.CreateException(nameof(func), error);
            }

            return result;
        }
    }
}

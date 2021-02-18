using Hik.Api.Abstraction;
using Hik.Api.Helpers;
using Hik.Api.Struct;
using Hik.Api.Struct.Video;
using System;
using System.Runtime.InteropServices;

namespace Hik.Api.Services
{
    public class HikVideoService : FileService
    {
        public virtual int StartDownloadFile(int userId, string sourceFile, string destinationPath)
        {
            int downloadHandle = SdkHelper.InvokeSDK(() => NET_DVR_GetFileByName(userId, sourceFile, destinationPath));

            uint iOutValue = 0;
            SdkHelper.InvokeSDK(() => NET_DVR_PlayBackControl_V40(downloadHandle, HikConst.NET_DVR_PLAYSTART, IntPtr.Zero, 0, IntPtr.Zero, ref iOutValue));
            return downloadHandle;
        }

        public virtual int GetDownloadPosition(int fileHandle)
        {
            return SdkHelper.InvokeSDK(() => NET_DVR_GetDownloadPos(fileHandle));
        }

        public virtual void StopDownloadFile(int fileHandle)
        {
            SdkHelper.InvokeSDK(() => NET_DVR_StopGetFile(fileHandle));
        }

        protected override bool FindClose(int findId)
        {
            return SdkHelper.InvokeSDK(() => NET_DVR_FindClose_V30(findId));
        }

        internal override int FindNext(int findId, ref ISourceFile source)
        {
            NET_DVR_FINDDATA_V30 findData = default(NET_DVR_FINDDATA_V30);
            int res = SdkHelper.InvokeSDK(() => NET_DVR_FindNextFile_V30(findId, ref findData));

            source = findData;
            return res;
        }

        protected override int StartFind(int userId, DateTime periodStart, DateTime periodEnd, int channel)
        {
            NET_DVR_FILECOND_V40 findConditions = new NET_DVR_FILECOND_V40
            {
                lChannel = channel,
                dwFileType = 0xff, // all
                dwIsLocked = 0xff, // all, locked and unlocked
                struStartTime = new NET_DVR_TIME(periodStart),
                struStopTime = new NET_DVR_TIME(periodEnd),
            };
            return SdkHelper.InvokeSDK(() => NET_DVR_FindFile_V40(userId, ref findConditions));
        }

        [DllImport(HikApi.DllPath)]
        private static extern bool NET_DVR_FindClose_V30(int lFindHandle);

        [DllImport(HikApi.DllPath)]
        private static extern int NET_DVR_FindNextFile_V30(int lFindHandle, ref NET_DVR_FINDDATA_V30 lpFindData);

        [DllImport(HikApi.DllPath)]
        private static extern int NET_DVR_FindFile_V40(int lUserID, ref NET_DVR_FILECOND_V40 pFindCond);

        [DllImport(HikApi.DllPath)]
        private static extern int NET_DVR_GetFileByName(int lUserID, string sDVRFileName, string sSavedFileName);

        [DllImport(HikApi.DllPath)]
        private static extern bool NET_DVR_PlayBackControl_V40(int lPlayHandle, uint dwControlCode, IntPtr lpInBuffer, uint dwInValue, IntPtr lpOutBuffer, ref uint lPOutValue);

        [DllImport(HikApi.DllPath)]
        private static extern int NET_DVR_GetDownloadPos(int lFileHandle);

        [DllImport(HikApi.DllPath)]
        private static extern bool NET_DVR_StopGetFile(int lFileHandle);
    }
}

using Hik.Api.Abstraction;
using Hik.Api.Helpers;
using Hik.Api.Struct;
using Hik.Api.Struct.Photo;
using System;
using System.Runtime.InteropServices;


namespace Hik.Api.Services
{
    public class HikPhotoService : FileService
    {
        public virtual void DownloadFile(int userId, string remoteFileName, long size, string destinationPath)
        {
            if (size > 0)
            {
                NET_DVR_PIC_PARAM temp = new NET_DVR_PIC_PARAM
                {
                    pDVRFileName = remoteFileName,
                    pSavedFileBuf = Marshal.AllocHGlobal((int)size),
                    dwBufLen = (uint)size
                };

                if (SdkHelper.InvokeSDK(() => NET_DVR_GetPicture_V50(userId, ref temp)))
                {
                    SdkHelper.InvokeSDK(() => NET_DVR_GetPicture(userId, temp.pDVRFileName, destinationPath));
                }

                Marshal.FreeHGlobal(temp.pSavedFileBuf);
            }
        }

        protected override bool FindClose(int findId)
        {
            return SdkHelper.InvokeSDK(() => NET_DVR_CloseFindPicture(findId));
        }

        internal override int FindNext(int findId, ref ISourceFile source)
        {
            NET_DVR_FIND_PICTURE_V50 findData = new NET_DVR_FIND_PICTURE_V50();

            int res = SdkHelper.InvokeSDK(() => NET_DVR_FindNextPicture_V50(findId, ref findData));
            source = findData;

            return res;
        }

        protected override int StartFind(int userId, DateTime periodStart, DateTime periodEnd, int channel)
        {

            NET_DVR_FIND_PICTURE_PARAM findConditions = new NET_DVR_FIND_PICTURE_PARAM
            {
                lChannel = channel,
                byFileType = 0xff, // all
                struStartTime = new NET_DVR_TIME(periodStart),
                struStopTime = new NET_DVR_TIME(periodEnd)
            };

            return SdkHelper.InvokeSDK(() => NET_DVR_FindPicture(userId, ref findConditions));
        }

        [DllImport(HikApi.DllPath)]
        private static extern int NET_DVR_FindPicture(int lUserID, ref NET_DVR_FIND_PICTURE_PARAM pFindParam);

        [DllImport(HikApi.DllPath)]
        private static extern int NET_DVR_FindNextPicture_V50(int lFindHandle, ref NET_DVR_FIND_PICTURE_V50 lpFindData);

        [DllImport(HikApi.DllPath)]
        private static extern bool NET_DVR_CloseFindPicture(int lpFindHandle);

        [DllImport(HikApi.DllPath)]
        private static extern bool NET_DVR_GetPicture_V50(int lUserID, ref NET_DVR_PIC_PARAM lpPicParam);

        [DllImport(HikApi.DllPath)]
        private static extern bool NET_DVR_GetPicture(int lUserID, string sDVRFileName, [In] [MarshalAs(UnmanagedType.LPStr)] string sSavedFileName);
    }
}

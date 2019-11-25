using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace HikApi
{
    [ExcludeFromCodeCoverage]
    public static class Api
    {
        private const string DllPath = @"SDK\HCNetSDK";

        [DllImport(DllPath)]
        public static extern int NET_DVR_FindFile_V40(int lUserID, ref Struct.NET_DVR_FILECOND_V40 pFindCond);

        [DllImport(DllPath)]
        public static extern uint NET_DVR_GetLastError();

        [DllImport(DllPath)]
        public static extern int NET_DVR_FindNextFile_V30(int lFindHandle, ref Struct.NET_DVR_FINDDATA_V30 lpFindData);

        [DllImport(DllPath)]
        public static extern bool NET_DVR_StopGetFile(int lFileHandle);

        [DllImport(DllPath)]
        public static extern int NET_DVR_GetDownloadPos(int lFileHandle);

        [DllImport(DllPath)]
        public static extern bool NET_DVR_PlayBackControl_V40(int lPlayHandle, uint dwControlCode, IntPtr lpInBuffer, uint dwInValue, IntPtr lpOutBuffer, ref uint lPOutValue);

        [DllImport(DllPath)]
        public static extern int NET_DVR_GetFileByName(int lUserID, string sDVRFileName, string sSavedFileName);

        [DllImport(DllPath)]
        public static extern bool NET_DVR_Init();

        [DllImport(DllPath)]
        public static extern bool NET_DVR_SetLogToFile(int bLogEnable, string strLogDir, bool bAutoDel);

        [DllImport(DllPath)]
        public static extern bool NET_DVR_Logout(int iUserID);

        [DllImport(DllPath)]
        public static extern bool NET_DVR_FindClose_V30(int lFindHandle);

        [DllImport(DllPath)]
        public static extern bool NET_DVR_Cleanup();

        [DllImport(DllPath)]
        public static extern int NET_DVR_Login_V30(string sDVRIP, int wDVRPort, string sUserName, string sPassword, ref Struct.NET_DVR_DEVICEINFO_V30 lpDeviceInfo);
    }
}
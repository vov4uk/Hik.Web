using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Hik.Api.Abstraction;
using Hik.Api.Data;
using Hik.Api.Helpers;
using Hik.Api.Services;
using Hik.Api.Struct;
using Hik.Api.Struct.Config;

namespace Hik.Api
{
    [ExcludeFromCodeCoverage]
    public class HikApi : IHikApi
    {
        private HikVideoService videoService;
        private HikPhotoService pictureService;

        public const string DllPath = @"SDK\HCNetSDK";



        public HikVideoService VideoService
        {
            get
            {
                return videoService ??= new HikVideoService();
            }
        }

        public HikPhotoService PhotoService
        {
            get
            {
                return pictureService ??= new HikPhotoService();
            }
        }

        public bool Initialize()
        {
            return SdkHelper.InvokeSDK(() => NET_DVR_Init());
        }

        public bool SetConnectTime(uint waitTimeMilliseconds, uint tryTimes)
        {
            return SdkHelper.InvokeSDK(() => NET_DVR_SetConnectTime(waitTimeMilliseconds, tryTimes)); // 2000 , 1
        }

        public bool SetReconnect(uint interval, int enableRecon)
        {
            return SdkHelper.InvokeSDK(() => NET_DVR_SetReconnect(interval, enableRecon)); // 10000 , 1
        }

        /// <summary>Setups the logs.</summary>
        /// <param name="logLevel">Log level. 0- close log(default), 1- output ERROR log only, 2- output ERROR and DEBUG log, 3- output all log, including ERROR, DEBUG and INFO log</param>
        /// <param name="logDirectory">The log directory. Log file saving path, if set to NULL, the default path for Windows is "C:\\SdkLog\\", and the default path for Linux is ""/home/sdklog/"</param>
        /// <param name="autoDelete">Whether to delete the files which exceed the number limit. Default: TRUE.</param>
        /// <returns>bool</returns>
        public bool SetupLogs(int logLevel, string logDirectory, bool autoDelete)
        {
            return SdkHelper.InvokeSDK(() => NET_DVR_SetLogToFile(logLevel, logDirectory, autoDelete));
        }

        public Session Login(string ipAddress, int port, string userName, string password)
        {
            NET_DVR_DEVICEINFO_V30 deviceInfo = default;
            int userId = SdkHelper.InvokeSDK(() => NET_DVR_Login_V30(ipAddress, port, userName, password, ref deviceInfo));
            return new Session(userId, deviceInfo.byChanNum);
        }

        public void Cleanup()
        {
            SdkHelper.InvokeSDK(() => NET_DVR_Cleanup());
        }

        public void Logout(int userId)
        {
            SdkHelper.InvokeSDK(() => NET_DVR_Logout(userId));
        }

        public HdInfo GetHddStatus(int userId)
        {

            NET_DVR_HDCFG hdConfig = default;
            uint returned = 0;
            int sizeOfConfig = Marshal.SizeOf(hdConfig);
            IntPtr ptrDeviceCfg = Marshal.AllocHGlobal(sizeOfConfig);
            Marshal.StructureToPtr(hdConfig, ptrDeviceCfg, false);
            SdkHelper.InvokeSDK(() => NET_DVR_GetDVRConfig(
                userId,
                HikConst.NET_DVR_GET_HDCFG,
                -1,
                ptrDeviceCfg,
                (uint)sizeOfConfig,
                ref returned));

            hdConfig = (NET_DVR_HDCFG)Marshal.PtrToStructure(ptrDeviceCfg, typeof(NET_DVR_HDCFG));
            Marshal.FreeHGlobal(ptrDeviceCfg);
            return new HdInfo(hdConfig.struHDInfo[0]);
        }


        [DllImport(DllPath)]
        private static extern bool NET_DVR_Init();

        [DllImport(DllPath)]
        private static extern bool NET_DVR_SetLogToFile(int bLogEnable, string strLogDir, bool bAutoDel);

        [DllImport(DllPath)]
        private static extern int NET_DVR_Login_V30(string sDVRIP, int wDVRPort, string sUserName, string sPassword, ref NET_DVR_DEVICEINFO_V30 lpDeviceInfo);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_Logout(int iUserID);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_Cleanup();

        [DllImport(DllPath)]
        private static extern bool NET_DVR_SetConnectTime(uint dwWaitTime, uint dwTryTimes);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_SetReconnect(uint dwInterval, int bEnableRecon);

        [DllImport(DllPath)]
        private static extern bool NET_DVR_GetDVRConfig(int lUserID, uint dwCommand, int lChannel, IntPtr lpOutBuffer, uint dwOutBufferSize, ref uint lpBytesReturned);
    }
}
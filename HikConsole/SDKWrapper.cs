using HikConsole.Abstraction;

namespace HikConsole
{
    public class SDKWrapper : ISDKWrapper
    {
        public bool Initialize()
        {
            if (!SDK.NET_DVR_Init())
            {
                throw new System.Exception(nameof(SDK.NET_DVR_Init));
            }
            return true;
        }

        public bool SetupSDKLogs(int bLogEnable, string strLogDir, bool bAutoDel)
        {
            if (!SDK.NET_DVR_SetLogToFile(bLogEnable, strLogDir, bAutoDel))
            {
                throw new System.Exception(nameof(SDK.NET_DVR_SetLogToFile));
            }
            return true;            
        }

        public int Login(string ipAdress, int port, string userName, string password, ref DeviceInfo deviceInfo)
        {
            SDK.NET_DVR_DEVICEINFO_V30 lpDeviceInfo = default;
            int _userId = SDK.NET_DVR_Login_V30(ipAdress, port, userName, password, ref lpDeviceInfo);
            if (_userId < 0)
            {
                throw new System.Exception(nameof(SDK.NET_DVR_Login_V30));
            }
            deviceInfo = new DeviceInfo(lpDeviceInfo);
            return _userId;
        }
    }
}

namespace HikConsole.Abstraction
{
    public interface ISDKWrapper
    {
        bool Initialize();

        bool SetupSDKLogs(int bLogEnable, string strLogDir, bool bAutoDel);

        int Login(string ipAdress, int port, string userName, string password, ref DeviceInfo deviceInfo);
    }
}

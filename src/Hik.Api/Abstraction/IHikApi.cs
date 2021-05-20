using Hik.Api.Data;
using Hik.Api.Services;

namespace Hik.Api.Abstraction
{
    public interface IHikApi
    {
        HikVideoService VideoService { get; }

        HikPhotoService PhotoService { get; }

        bool Initialize();

        bool SetConnectTime(uint waitTimeMilliseconds, uint tryTimes);

        bool SetReconnect(uint interval, int enableRecon);

        bool SetupLogs(int logLevel, string logDirectory, bool autoDelete);

        Session Login(string ipAddress, int port, string userName, string password);

        HdInfo GetHddStatus(int userId);

        void Logout(int userId);

        void Cleanup();
    }
}

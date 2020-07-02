using HikConsole.Config;

namespace HikConsole.Abstraction
{
    public interface IHikConfig
    {
        AppConfig GetConfig(string configFileName);
    }
}

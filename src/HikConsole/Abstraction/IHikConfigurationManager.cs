using HikConsole.Config;

namespace HikConsole.Abstraction
{
    public interface IHikConfigurationManager
    {
        AppConfig Config { get; }
    }
}

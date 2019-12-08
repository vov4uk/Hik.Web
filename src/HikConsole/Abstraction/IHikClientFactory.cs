using HikConsole.Config;

namespace HikConsole.Abstraction
{
    public interface IHikClientFactory
    {
        IHikClient Create(CameraConfig camera);
    }
}

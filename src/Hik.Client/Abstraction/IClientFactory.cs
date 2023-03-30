using Hik.DTO.Config;
using Serilog;

namespace Hik.Client.Abstraction
{
    public interface IClientFactory
    {
        IDownloaderClient Create(CameraConfig camera, ILogger logger);
    }
}

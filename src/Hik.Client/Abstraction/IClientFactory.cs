using Hik.DTO.Config;

namespace Hik.Client.Abstraction
{
    public interface IClientFactory
    {
        IDownloaderClient Create(CameraConfig camera);
    }
}

using Hik.DTO.Config;

namespace Hik.Client.Abstraction
{
    public interface IClientFactory
    {
        IClient Create(CameraConfig camera);
    }
}

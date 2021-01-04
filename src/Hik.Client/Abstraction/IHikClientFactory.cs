using Hik.DTO.Config;

namespace Hik.Client.Abstraction
{
    public interface IHikClientFactory
    {
        IHikClient Create(CameraConfig camera);
    }
}

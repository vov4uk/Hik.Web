using Hik.DTO.Config;

namespace Job
{
    public interface IConfig<T>
        where T : BaseConfig
    {
        T GetConfig();
    }
}

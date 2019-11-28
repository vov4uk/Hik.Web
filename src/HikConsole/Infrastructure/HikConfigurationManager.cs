using HikConsole.Abstraction;
using HikConsole.Config;
using Newtonsoft.Json;

namespace HikConsole.Infrastructure
{
    public class HikConfigurationManager : IHikConfigurationManager
    {
        public HikConfigurationManager(IFilesHelper fileHelper)
        {
            this.Config = JsonConvert.DeserializeObject<AppConfig>(fileHelper.ReadAllText("configuration.json"));
        }

        public AppConfig Config { get; }
    }
}

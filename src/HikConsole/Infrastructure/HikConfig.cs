using System;
using System.Diagnostics.CodeAnalysis;
using HikConsole.Abstraction;
using HikConsole.Config;
using Newtonsoft.Json;

namespace HikConsole.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class HikConfig : IHikConfig
    {
        private readonly Lazy<AppConfig> lazyAppConfig;

        public HikConfig(IFilesHelper fileHelper)
        {
            this.lazyAppConfig = new Lazy<AppConfig>(() => JsonConvert.DeserializeObject<AppConfig>(fileHelper.ReadAllText("configuration.json")));
        }

        public AppConfig Config
        {
            get
            {
                return this.lazyAppConfig.Value;
            }
        }
    }
}
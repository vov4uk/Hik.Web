using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;

namespace Job.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class HikConfigExtensions
    {
        public static T GetConfig<T>(string configPath = "configuration.json")
        {
#if DEBUG
            configPath = configPath.Replace(".json", ".debug.json");
#endif

            var config = JsonConvert.DeserializeObject<T>(File.ReadAllText(configPath));
            if(config == null)
            {
                throw new InvalidCastException($"Config {configPath} invalid");
            }
            return config;
        }
    }
}
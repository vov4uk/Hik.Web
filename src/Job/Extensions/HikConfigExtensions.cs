using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;

namespace Job.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class HikConfigExtensions
    {
        public static T GetConfig<T>(string configFileName = "configuration.json")
        {
            var config = JsonConvert.DeserializeObject<T>(File.ReadAllText(configFileName));
            if(config == null)
            {
                throw new InvalidCastException($"Config {configFileName} invalid");
            }
            return config;
        }

        public static string GetConfigPath(string configFileName)
        {
#if DEBUG
            configFileName = configFileName.Replace(".json", ".debug.json");
#endif
            return Path.Combine(Environment.CurrentDirectory, "Config", configFileName);
        }
    }
}
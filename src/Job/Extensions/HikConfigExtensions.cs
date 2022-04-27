using System;
using System.IO;
using Newtonsoft.Json;

namespace Job.Extensions
{
    public static class HikConfigExtensions
    {
        public static T GetConfig<T>(string configFileName) =>
            JsonConvert.DeserializeObject<T>(File.ReadAllText(configFileName));

        public static string GetConfigPath(string configFileName)
        {
#if DEBUG
            configFileName = configFileName.Replace(".json", ".debug.json");
#endif
            return Path.Combine(Environment.CurrentDirectory, "Config", configFileName);
        }
    }
}
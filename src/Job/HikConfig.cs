using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public class HikConfig
    {
        public static T GetConfig<T>(string configFileName = "configuration.json")
        {
#if DEBUG
            configFileName = configFileName.Replace(".json", ".debug.json");
#endif

            string configPath = Path.Combine(GetAssemblyDirectory(), configFileName);
            var config = JsonConvert.DeserializeObject<T>(File.ReadAllText(configPath));
            if(config == null)
            {
                throw new NullReferenceException($"Config {configFileName} invalid");
            }
            return config;
        }

        private static string GetAssemblyDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().Location;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}
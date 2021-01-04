using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Hik.DTO.Config;
using Newtonsoft.Json;

namespace Job
{
    [ExcludeFromCodeCoverage]
    public class HikConfig
    {
        public static AppConfig GetConfig(string configFileName = "configuration.json")
        {
            string configPath = Path.Combine(GetAssemblyDirectory(), configFileName);
            var appConfig = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(configPath));
            if(appConfig.Camera == null)
            {
                throw new NullReferenceException("Camera config invalid");
            }
            return appConfig;
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
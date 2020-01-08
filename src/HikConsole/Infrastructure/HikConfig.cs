using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
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
            string configPath = fileHelper.CombinePath(AssemblyDirectory, "configuration.json");
            this.lazyAppConfig = new Lazy<AppConfig>(() => JsonConvert.DeserializeObject<AppConfig>(fileHelper.ReadAllText(configPath)));
        }

        public AppConfig Config => this.lazyAppConfig.Value;

        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
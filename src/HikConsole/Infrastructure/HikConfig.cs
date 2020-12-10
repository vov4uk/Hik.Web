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
        private readonly IFilesHelper fileHelper;

        public HikConfig(IFilesHelper fileHelper)
        {
            this.fileHelper = fileHelper;
        }

        public AppConfig GetConfig(string configFileName = "configuration.json")
        {
            string configPath = this.fileHelper.CombinePath(GetAssemblyDirectory(), configFileName);
            return JsonConvert.DeserializeObject<AppConfig>(this.fileHelper.ReadAllText(configPath));
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
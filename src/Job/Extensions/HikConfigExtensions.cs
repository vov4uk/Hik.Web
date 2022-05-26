using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Hik.DTO.Config;
using Newtonsoft.Json;

namespace Job.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class HikConfigExtensions
    {
        public static T GetConfig<T>(string configFilePath)
        {
            if (File.Exists(configFilePath))
            {
                return JsonConvert.DeserializeObject<T>(File.ReadAllText(configFilePath));
            }
            return default(T);
        }

        public static string GetConfigPath(string configFileName)
        {
#if DEBUG
            configFileName = configFileName.Replace(".json", ".debug.json");
#endif
            return Path.Combine(Environment.CurrentDirectory, "Config", configFileName);
        }

        public static (DateTime PeriodStart, DateTime PeriodEnd) CalculateProcessingPeriod(BaseConfig config, DateTime? lastSync)
        {
            var cameraConfig = config as CameraConfig;
            DateTime jobStart = DateTime.Now;

            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * cameraConfig?.ProcessingPeriodHours ?? 1);

            return(periodStart, jobStart);
        }
    }
}
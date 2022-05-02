using System;
using System.IO;
using Hik.DTO.Config;
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

        public static (DateTime PeriodStart, DateTime PeriodEnd) CalculateProcessingPeriod(BaseConfig config, DateTime? lastSync)
        {
            var cameraConfig = config as CameraConfig;
            DateTime jobStart = DateTime.Now;

            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * cameraConfig?.ProcessingPeriodHours ?? 1);

            return(periodStart, jobStart);
        }
    }
}
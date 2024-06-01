using System;
using System.Diagnostics.CodeAnalysis;
using Hik.DTO.Config;
using Newtonsoft.Json;

namespace Job.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class HikConfigExtensions
    {
        public static T GetConfig<T>(string json)
        {
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (Exception)
                {
                }
            }
            return default(T);
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
using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.Quartz.Contracts
{
    public class CronConfigDto
    {
        public CronConfigDto() { }

        public CronConfigDto(CronDto cron)
        {
            Path = cron.ConfigPath;
            JobName = cron.Name;
        }

        public string JobName { get; set; }

        [DataType(DataType.MultilineText)]
        public string Json { get; set; }

        public string Path { get; set; }

        public string GetConfigPath()
        {
            var configFileName = Path;
#if DEBUG
            configFileName = Path.Replace(".json", ".debug.json");
#endif
            return System.IO.Path.Combine(Environment.CurrentDirectory, "Config", configFileName);
        }
    }
}

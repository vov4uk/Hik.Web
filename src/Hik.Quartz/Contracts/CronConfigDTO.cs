using Hik.Quartz.Contracts.Xml;
using Hik.Quartz.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Hik.Quartz.Contracts
{
    public class CronConfigDTO
    {
        public CronConfigDTO() { }

        public CronConfigDTO(Cron cron)
        {
            var configFileName = cron.Jobdatamap.Entry.FirstOrDefault(x => x.Key == CronTriggerImplExtensions.Config)?.Value;
            Path = configFileName;
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

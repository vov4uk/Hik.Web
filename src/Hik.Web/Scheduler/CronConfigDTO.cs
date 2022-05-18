using Job.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Hik.Web.Scheduler
{
    public class CronConfigDTO
    {
        public string Path { get; set; }
        public string JobName { get; set; }

        [DataType(DataType.MultilineText)]
        public string Json { get; set; }

        public CronConfigDTO() { }
        public CronConfigDTO(Cron cron)
        {
            var configFileName = cron.Jobdatamap.Entry.FirstOrDefault(x => x.Key == CronTriggerImplExtensions.Config)?.Value;
            Path = HikConfigExtensions.GetConfigPath(configFileName);
            JobName = cron.Name;
        }
    }
}

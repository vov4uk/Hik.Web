using Hik.Quartz.Contracts;

namespace Hik.Web.Queries.QuartzJobConfig
{
    public class QuartzJobConfigDto : IHandlerResult
    {
        public CronConfigDTO ConfigDTO { get; set; }
    }
}

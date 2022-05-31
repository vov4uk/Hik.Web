using Hik.Quartz.Contracts;

namespace Hik.Web.Queries.QuartzJobConfig
{
    public class QuartzJobConfigDto : IHandlerResult
    {
        public CronConfigDto Config { get; set; }
    }
}

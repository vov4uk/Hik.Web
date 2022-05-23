using Hik.Quartz.Contracts;

namespace Hik.Web.Queries.QuartzJob
{
    public class QuartzJobDto : IHandlerResult
    {
       public CronDTO Cron { get; set; }
    }
}

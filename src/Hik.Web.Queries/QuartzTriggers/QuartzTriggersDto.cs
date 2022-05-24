using Hik.Quartz.Contracts;

namespace Hik.Web.Queries.QuartzTriggers
{
    public class QuartzTriggersDto : IHandlerResult
    {
        public IReadOnlyCollection<CronDto> Items { get; set; }
    }
}

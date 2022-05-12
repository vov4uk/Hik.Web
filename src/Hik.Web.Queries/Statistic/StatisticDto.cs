using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Statistic
{
    public class StatisticDto : IHandlerResult
    {
        public int JobTriggerId { get; set; }

        public string JobTriggerName { get; set; }

        public int TotalItems { get; set; }

        public IReadOnlyCollection<DailyStatisticDto> Days { get; set; }
    }
}

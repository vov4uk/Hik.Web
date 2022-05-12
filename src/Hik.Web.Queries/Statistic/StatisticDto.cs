using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Statistic
{
    public class StatisticDto : ResponseBase<DailyStatisticDto>
    {
        public int JobTriggerId { get; set; }

        public string JobTriggerName { get; set; }
    }
}

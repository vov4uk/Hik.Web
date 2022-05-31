using Hik.DTO.Contracts;

namespace Hik.Web.Queries.DashboardDetails
{
    public class DashboardDetailsDto : ResponseBase<DailyStatisticDto>
    {
        public int JobTriggerId { get; set; }

        public string JobTriggerName { get; set; }
    }
}

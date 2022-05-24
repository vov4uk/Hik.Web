using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Dashboard
{
    public class DashboardDto : IHandlerResult
    {
        public IReadOnlyCollection<DailyStatisticDto> DailyStatistics { get; set; }

        public Dictionary<int, DateTime> Files { get; set; }

        public IEnumerable<TriggerDto> Triggers { get; set; }
    }
}

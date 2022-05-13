using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Dashboard
{
    public class DashboardDto : IHandlerResult
    {
        public IReadOnlyCollection<DailyStatisticDto> Items { get; set; }

        public Dictionary<int, DateTime> Files { get; set; }

        public IEnumerable<TriggerDTO> Triggers { get; set; }
    }
}

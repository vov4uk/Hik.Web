using Hik.DTO.Contracts;

namespace Hik.Web.Queries.JobTriggers
{
    public class JobTriggersDto : IHandlerResult
    {
        public IReadOnlyCollection<TriggerDTO> Items { get; set; }
    }
}

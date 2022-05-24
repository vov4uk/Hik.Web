using Hik.DTO.Contracts;

namespace Hik.Web.Queries.JobTriggers
{
    public class JobTriggersDto : IHandlerResult
    {
        public IReadOnlyCollection<TriggerDto> Items { get; set; }
    }
}

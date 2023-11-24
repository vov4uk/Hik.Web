using Hik.DTO.Contracts;

namespace Hik.Web.Queries.QuartzTriggers
{
    public class QuartzTriggersDto : IHandlerResult
    {
        public IReadOnlyCollection<TriggerDto> Triggers { get; set; }
    }
}

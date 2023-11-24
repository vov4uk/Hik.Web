using Hik.DTO.Contracts;

namespace Hik.Web.Queries.QuartzTrigger
{
    public class QuartzTriggerDto : IHandlerResult
    {
        public TriggerDto Trigger { get; set; }
    }
}

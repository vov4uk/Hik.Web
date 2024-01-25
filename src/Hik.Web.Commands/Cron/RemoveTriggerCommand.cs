using MediatR;

namespace Hik.Web.Commands.Cron
{
    public class RemoveTriggerCommand : IRequest
    {
        public int TriggerId { get; set; }
    }
}

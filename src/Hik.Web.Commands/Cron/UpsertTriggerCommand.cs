using Hik.DTO.Contracts;
using MediatR;

namespace Hik.Web.Commands.Cron
{
    public class UpsertTriggerCommand : IRequest
    {
        public TriggerDto Trigger { get; set; }
    }
}

using Hik.DTO.Contracts;
using MediatR;

namespace Hik.Web.Commands.Cron
{
    public class UpsertTriggerCommand : IRequest<int>
    {
        public TriggerDto Trigger { get; set; }
    }
}

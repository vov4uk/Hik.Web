using MediatR;

namespace Hik.Web.Commands.Cron
{
    public class UpdateTriggerConfigCommand : IRequest
    {
        public int TriggerId { get; set; }

        public string JsonConfig { get; set; }
    }
}

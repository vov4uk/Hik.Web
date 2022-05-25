using Hik.Quartz.Contracts;
using MediatR;

namespace Hik.Web.Commands.Cron
{
    public class UpdateQuartzJobCommand : IRequest
    {
        public CronDto Cron { get; set; }
    }
}

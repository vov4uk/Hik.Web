using Hik.Quartz.Contracts;
using MediatR;

namespace Hik.Web.Commands.QuartzJob
{
    public class UpdateQuartzJobCommand : IRequest
    {
        public CronDTO Cron { get; set; }
    }
}

using Hik.Quartz.Contracts;
using MediatR;

namespace Hik.Web.Commands.Cron
{
    public class DeleteQuartzJobCommand : IRequest
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string ClassName { get; set; }
    }
}

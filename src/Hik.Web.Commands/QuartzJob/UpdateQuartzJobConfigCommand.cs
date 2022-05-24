using Hik.Quartz.Contracts;
using MediatR;

namespace Hik.Web.Commands.QuartzJob
{
    public class UpdateQuartzJobConfigCommand : IRequest
    {
        public string Path { get; set; }
        public string Json { get; set; }
    }
}

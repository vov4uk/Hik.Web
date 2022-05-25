using MediatR;

namespace Hik.Web.Commands.Config
{
    public class UpdateQuartzJobConfigCommand : IRequest
    {
        public string Path { get; set; }
        public string Json { get; set; }
    }
}

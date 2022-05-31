using MediatR;

namespace Hik.Web.Commands.Cron
{
    public class StartActivityCommand : IRequest<int>
    {
        public string Group { get; set; }
        public string Name { get; set; }
    }
}

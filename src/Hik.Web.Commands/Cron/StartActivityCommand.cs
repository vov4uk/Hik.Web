using MediatR;

namespace Hik.Web.Commands.Cron
{
    public class StartActivityCommand : IRequest
    {
        public string Environment { get; set; }
        public string AppConfigsPath { get; set; }
        public string Group { get; set; }
        public string Name { get; set; }
    }
}

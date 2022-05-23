using MediatR;

namespace Hik.Web.Queries.QuartzJob
{
    public class QuartzJobQuery : IRequest<IHandlerResult>
    {
        public string Name { get; set; }
        public string Group { get; set; }
    }
}

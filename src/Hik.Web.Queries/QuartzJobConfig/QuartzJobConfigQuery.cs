using MediatR;

namespace Hik.Web.Queries.QuartzJobConfig
{
    public class QuartzJobConfigQuery : IRequest<IHandlerResult>
    {
        public string Name { get; set; }
        public string Group { get; set; }
    }
}

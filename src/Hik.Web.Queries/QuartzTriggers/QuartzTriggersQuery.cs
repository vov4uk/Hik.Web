using MediatR;

namespace Hik.Web.Queries.QuartzTriggers
{
    public class QuartzTriggersQuery : IRequest<IHandlerResult>
    {
        public bool ActiveOnly { get; set; } = false;

        public bool IncludeLastJob { get; set; } = false;
    }
}

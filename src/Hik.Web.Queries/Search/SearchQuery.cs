using MediatR;

namespace Hik.Web.Queries.Search
{
    public class SearchQuery : IRequest<IHandlerResult>
    {
        public int JobTriggerId { get; set; }

        public DateTime? DateTime { get; set; }
    }
}

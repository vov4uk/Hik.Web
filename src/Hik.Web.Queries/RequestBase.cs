using MediatR;

namespace Hik.Web.Queries
{
    public abstract class RequestBase : IRequest<IHandlerResult>
    {
        public int PageSize { get; set; } = 40;

        public int MaxPages { get; set; } = 10;

        public int CurrentPage { get; set; }
    }
}

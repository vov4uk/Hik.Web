using MediatR;

namespace Hik.Web.Queries.Play
{
    public class PlayQuery : IRequest<IHandlerResult>
    {
        public int FileId { get; set; }
    }
}

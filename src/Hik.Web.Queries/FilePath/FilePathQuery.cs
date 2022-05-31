using MediatR;

namespace Hik.Web.Queries.FilePath
{
    public class FilePathQuery : IRequest<IHandlerResult>
    {
        public int FileId { get; set; }
    }
}

using MediatR;

namespace Hik.Web.Queries.Photo
{
    public class PhotoThumbnailQuery : IRequest<IHandlerResult>
    {
        public int FileId { get; set; }
    }
}

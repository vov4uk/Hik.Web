using MediatR;

namespace Hik.Web.Queries.Thumbnail
{
    public class PhotoThumbnailQuery : IRequest<IHandlerResult>
    {
        public int FileId { get; set; }
    }
}

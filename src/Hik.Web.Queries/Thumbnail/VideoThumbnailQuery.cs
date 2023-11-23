using MediatR;

namespace Hik.Web.Queries.Thumbnail
{
    public class VideoThumbnailQuery : IRequest<IHandlerResult>
    {
        public int FileId { get; set; }
    }
}

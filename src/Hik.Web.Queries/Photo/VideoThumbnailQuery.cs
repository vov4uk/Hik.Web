using MediatR;

namespace Hik.Web.Queries.Photo
{
    public class VideoThumbnailQuery : IRequest<IHandlerResult>
    {
        public int FileId { get; set; }
    }
}

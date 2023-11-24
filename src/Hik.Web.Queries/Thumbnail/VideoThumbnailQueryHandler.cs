using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Helpers.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries.Thumbnail
{
    public class VideoThumbnailQueryHandler : QueryHandler<VideoThumbnailQuery>
    {
        private readonly IUnitOfWorkFactory factory;
        private readonly IVideoHelper helper;

        public VideoThumbnailQueryHandler(IUnitOfWorkFactory factory, IVideoHelper helper)
        {
            this.factory = factory;
            this.helper = helper;
        }

        protected override async Task<IHandlerResult> HandleAsync(VideoThumbnailQuery request, CancellationToken cancellationToken)
        {
            string path = null;
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                IBaseRepository<MediaFile> filesRepo = uow.GetRepository<MediaFile>();
                var file = await filesRepo.FindByIdAsync(request.FileId);
                if (file != null)
                {
                    path = file.GetPath();
                }
            }

            string bytes = await helper.GetThumbnailStringAsync(path, 216, 122);

            return new VideoThumbnailDto
            {
                Id = request.FileId,
                Poster = bytes
            };
        }
    }
}

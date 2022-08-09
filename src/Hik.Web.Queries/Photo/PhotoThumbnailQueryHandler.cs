using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Helpers.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries.Photo
{
    public class PhotoThumbnailQueryHandler : QueryHandler<PhotoThumbnailQuery>
    {
        private readonly IUnitOfWorkFactory factory;
        private readonly IImageHelper imageHelper;

        public PhotoThumbnailQueryHandler(IUnitOfWorkFactory factory, IImageHelper imageHelper)
        {
            this.factory = factory;
            this.imageHelper = imageHelper;
        }

        protected override async Task<IHandlerResult> HandleAsync(PhotoThumbnailQuery request, CancellationToken cancellationToken)
        {
            string path = null;
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                IBaseRepository<MediaFile> filesRepo = uow.GetRepository<MediaFile>();
                var file = await filesRepo.FindByAsync(x => x.Id == request.FileId);
                if (file != null)
                { 
                    path = file.GetPath();
                }
            }

            byte[] bytes = imageHelper.GetThumbnail(path, 216, 122);

            return new PhotoThumbnailDto
            {
                Id = request.FileId,
                Poster = bytes
            };
        }
    }
}

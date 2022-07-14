using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;

namespace Hik.Web.Queries.Photo
{
    public class PhotoThumbnailQueryHandler : QueryHandler<PhotoThumbnailQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public PhotoThumbnailQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override async Task<IHandlerResult> HandleAsync(PhotoThumbnailQuery request, CancellationToken cancellationToken)
        {
            string path = null;
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                IBaseRepository<MediaFile> filesRepo = uow.GetRepository<MediaFile>();
                var file = await filesRepo.FindByAsync(x => x.Id == request.FileId);
                if (file != null) { path = file.GetPath(); }
            }
            //path = @"C:\Users\vkhmelovskyi\Desktop\Pic\074c3539.jpg";


            byte[] bytes = null;
#pragma warning disable CA1416 // Validate platform compatibility
            using (Image image = Image.FromFile(path))
            {
                using (MemoryStream m = new MemoryStream())
                {
                    using (Image thumb = image.GetThumbnailImage(216, 122, () => false, IntPtr.Zero))
                    {
                        thumb.Save(m, ImageFormat.Jpeg);
                            bytes = new byte[m.Length];
                            m.Position = 0;
                            m.Read(bytes, 0, bytes.Length);
                    }
                }
            }
#pragma warning restore CA1416 // Validate platform compatibility

            return new PhotoThumbnailDto
            {
                Id = request.FileId,
                Poster = bytes
            };
        }
    }
}

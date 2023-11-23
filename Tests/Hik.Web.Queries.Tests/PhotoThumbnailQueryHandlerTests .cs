using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Helpers.Abstraction;
using Hik.Web.Queries.Thumbnail;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Queries.Test
{
    public class PhotoThumbnailQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<MediaFile>> repoMock = new(MockBehavior.Strict);
        private readonly Mock<IImageHelper> imgMock = new(MockBehavior.Strict);

        public PhotoThumbnailQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<MediaFile>())
                .Returns(repoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundFile_ReturnThumbnail(PhotoThumbnailQuery request)
        {
            var expected = new byte[2] { 0 ,1 };
            repoMock.Setup(x => x.FindByIdAsync(It.IsAny<int>()))
                            .ReturnsAsync(new MediaFile() { Name = "File", Id = 2, Path = "c:\\file.jpg", Duration = 100, Date = new(2022, 01, 01) });
            imgMock.Setup(x => x.GetThumbnail(It.IsAny<string>(), 216, 122)).Returns(expected);

            var handler = new PhotoThumbnailQueryHandler(uowFactoryMock.Object, imgMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<PhotoThumbnailDto>(result);
            var dto = (PhotoThumbnailDto)result;
            Assert.Equal(expected, dto.Poster);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_NotFoundFile_ReturnNull(PhotoThumbnailQuery request)
        {
            repoMock.Setup(x => x.FindByIdAsync(It.IsAny<int>()))
                            .ReturnsAsync(default(MediaFile));
            imgMock.Setup(x => x.GetThumbnail(null, 216, 122)).Returns(default(byte[]));

            var handler = new PhotoThumbnailQueryHandler(uowFactoryMock.Object, imgMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<PhotoThumbnailDto>(result);
            var dto = (PhotoThumbnailDto)result;
            Assert.Null(dto.Poster);
        }
    }
}

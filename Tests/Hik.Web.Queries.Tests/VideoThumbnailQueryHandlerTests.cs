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
    public class VideoThumbnailQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<MediaFile>> repoMock = new(MockBehavior.Strict);
        private readonly Mock<IVideoHelper> helperMock = new(MockBehavior.Strict);

        public VideoThumbnailQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<MediaFile>())
                .Returns(repoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundFile_ReturnThumbnail(VideoThumbnailQuery request)
        {
            var expected = "base64";
            repoMock.Setup(x => x.FindByIdAsync(It.IsAny<int>()))
                            .ReturnsAsync(new MediaFile() { Name = "File", Id = 2, Path = "c:\\file.jpg", Duration = 100, Date = new(2022, 01, 01) });
            helperMock.Setup(x => x.GetThumbnailStringAsync(It.IsAny<string>(), 216, 122)).ReturnsAsync("base64");

            var handler = new VideoThumbnailQueryHandler(uowFactoryMock.Object, helperMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<VideoThumbnailDto>(result);
            var dto = (VideoThumbnailDto)result;
            Assert.Equal(expected, dto.Poster);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_NotFoundFile_ReturnNull(VideoThumbnailQuery request)
        {
            repoMock.Setup(x => x.FindByIdAsync(It.IsAny<int>()))
                            .ReturnsAsync(default(MediaFile));
            helperMock.Setup(x => x.GetThumbnailStringAsync(null, 216, 122)).ReturnsAsync(default(string));

            var handler = new VideoThumbnailQueryHandler(uowFactoryMock.Object, helperMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<VideoThumbnailDto>(result);
            var dto = (VideoThumbnailDto)result;
            Assert.Null(dto.Poster);
        }
    }
}

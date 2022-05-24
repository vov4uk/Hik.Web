using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.FilePath;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Queries.Test
{
    public class FilePathQueryHandlerTest
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<MediaFile>> repoMock = new(MockBehavior.Strict);

        public FilePathQueryHandlerTest()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<MediaFile>())
                .Returns(repoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundFile_ReturnPath(FilePathQuery request)
        {
            repoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(), It.IsAny<Expression<Func<MediaFile, object>>[]>()))
                .ReturnsAsync(new MediaFile() { Path = "c:\\file.jpg"});

            var handler = new FilePathQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<FilePathDto>(result);
            var dto = (FilePathDto)result;
            Assert.NotNull(dto.Path);
            Assert.Equal(request.FileId ,dto.Id);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundNotFile_ReturnNullPath(FilePathQuery request)
        {
            repoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(), It.IsAny<Expression<Func<MediaFile, object>>[]>()))
                .ReturnsAsync(default(MediaFile));

            var handler = new FilePathQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<FilePathDto>(result);
            var dto = (FilePathDto)result;
            Assert.Null(dto.Path);
            Assert.Equal(request.FileId ,dto.Id);
        }
    }
}
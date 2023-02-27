using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Helpers.Abstraction;
using Hik.Web.Queries.Play;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Queries.Test
{
    public class PlayQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<MediaFile>> repoMock = new(MockBehavior.Strict);
        private readonly Mock<IFilesHelper> filesHelper = new(MockBehavior.Strict);
        private readonly Mock<IVideoHelper> videoHelper = new(MockBehavior.Strict);

        public PlayQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<MediaFile>())
                .Returns(repoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
            videoHelper.SetupGet(x => x.DefaultPoster).Returns(string.Empty);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundFile_ReturnFile(PlayQuery request)
        {
            repoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(), It.IsAny<Expression<Func<MediaFile, object>>[]>()))
                .ReturnsAsync(new MediaFile() { Name = "File", Id = 2, Path = "c:\\file.jpg", Duration = 100, Date = new(2022,01,01) });
            repoMock.Setup(x => x.GetAll())
                .Returns(new List<MediaFile>().AsQueryable());
            filesHelper.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);
            videoHelper.Setup(x => x.GetThumbnailStringAsync("c:\\file.jpg", It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync("Poster");

            var handler = new PlayQueryHandler(uowFactoryMock.Object, filesHelper.Object, videoHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<PlayDto>(result);
            var dto = (PlayDto)result;
            Assert.Equal("File (01m 40s)", dto.FileTitle);
            Assert.Equal("Poster", dto.Poster);
            Assert.Equal("2022-01-01 00:01:40", dto.FileTo);
            Assert.Null(dto.NextFile);
            Assert.Null(dto.PreviousFile);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_NextAndPreviousFilesFound_ReturnFiles(PlayQuery request)
        {
            repoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(), It.IsAny<Expression<Func<MediaFile, object>>[]>()))
                .ReturnsAsync(new MediaFile() { JobTriggerId = 1, Name = "File", Id = 13, Path = "c:\\file.jpg", Duration = 100, Date = new(2022,01,01) });
            repoMock.Setup(x => x.GetAll())
                .Returns(new List<MediaFile>()
                {
                    new(){ Id = 10, JobTriggerId = 1, Date = new(2022, 01, 01), Name = "File1.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 11, JobTriggerId = 1, Date = new(2022, 01, 02), Name = "File2.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 12, JobTriggerId = 1, Date = new(2022, 01, 02), Name = "File2.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 13, JobTriggerId = 1, Date = new(2022, 01, 03), Name = "File3.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 14, JobTriggerId = 1, Date = new(2022, 01, 04), Name = "File4.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 15, JobTriggerId = 1, Date = new(2022, 01, 05), Name = "File5.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 16, JobTriggerId = 1, Date = new(2022, 01, 06), Name = "File6.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 17, JobTriggerId = 1, Date = new(2022, 01, 06), Name = "File6.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 21, JobTriggerId = 2, Date = new(2022, 01, 01), Name = "File1.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 22, JobTriggerId = 2, Date = new(2022, 01, 02), Name = "File2.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 23, JobTriggerId = 2, Date = new(2022, 01, 03), Name = "File3.mp4", Size = 10, Path = "C:\\"},
                }.AsQueryable());
            filesHelper.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);
            videoHelper.Setup(x => x.GetThumbnailStringAsync("c:\\file.jpg", It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync("Poster");

            var handler = new PlayQueryHandler(uowFactoryMock.Object, filesHelper.Object, videoHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<PlayDto>(result);
            var dto = (PlayDto)result;
            Assert.Equal("File (01m 40s)", dto.FileTitle);
            Assert.Equal("Poster", dto.Poster);
            Assert.Equal("2022-01-01 00:01:40", dto.FileTo);
            Assert.NotNull(dto.NextFile);
            Assert.NotNull(dto.PreviousFile);
            Assert.Equal(12, dto.PreviousFile.Id);
            Assert.Equal(14, dto.NextFile.Id);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundNotFile_ReturnNotFound(PlayQuery request)
        {
            repoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(), It.IsAny<Expression<Func<MediaFile, object>>[]>()))
                .ReturnsAsync(default(MediaFile));

            var handler = new PlayQueryHandler(uowFactoryMock.Object, filesHelper.Object, videoHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<PlayDto>(result);
            var dto = (PlayDto)result;
            Assert.Equal("Not found", dto.FileTitle);
            Assert.NotNull(dto.Poster);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FileNotExist_ReturnNotFound(PlayQuery request)
        {
            repoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(), It.IsAny<Expression<Func<MediaFile, object>>[]>()))
                .ReturnsAsync(new MediaFile() { Path = "", Name = ""});
            filesHelper.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);

            var handler = new PlayQueryHandler(uowFactoryMock.Object, filesHelper.Object, videoHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<PlayDto>(result);
            var dto = (PlayDto)result;
            Assert.Equal("Not found", dto.FileTitle);
            Assert.NotNull(dto.Poster);
        }
    }
}

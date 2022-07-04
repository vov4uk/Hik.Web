using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.Search;
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
    public class SearchQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<MediaFile>> repoMock = new(MockBehavior.Strict);

        public SearchQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<MediaFile>())
                .Returns(repoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [InlineData("Match", 60)]
        [InlineData("Out of range", 59)]
        public async Task HandleAsync_FoundFile_MsgMatch(string msg, int duration)
        {
            var request = new SearchQuery { DateTime = new(2022, 01, 01, 01, 00, 00), JobTriggerId = 1 };
            var file = new MediaFile() { Id = 1, Date = new(2022, 01, 01, 00, 59, 00), Duration = duration, Path = "C:\\", Name = "File1.mp4" };

            SetupGetMediaFilesDescAsync(new List<MediaFile>() { file }, 1);
            SetupGetMediaFilesDescAsync(new List<MediaFile>(), 5);
            SetupGetMediaFilesAscAsync(new List<MediaFile>(), 5);

            var handler = new SearchQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<SearchDto>(result);
            var dto = (SearchDto)result;

            Assert.Equal(msg, dto.Message);
            var firstFile = dto.InRange.First();
            Assert.Equal(1, firstFile.Id);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundFile_ResultsSorted(SearchQuery request)
        {
            var file = new MediaFile() { Id = 6, Date = new(2022, 01, 01, 00, 59, 00), Duration = 60, Path = "C:\\", Name = "File6.mp4" };

            SetupGetMediaFilesDescAsync(new List<MediaFile>() { file }, 1);

            repoMock.Setup(x => x.FindManyByDescAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(),
                It.IsAny<Expression<Func<MediaFile, object>>>(), 0, 5, x => x.DownloadDuration))
                .ReturnsAsync(new List<MediaFile>()
            {
                new(){Id = 5, Date = new(2022,02,02), Path = "C:\\", Name = "File5.jpg"},
                new(){Id = 4, Date = new(2022,02,01), Path = "C:\\", Name = "File4.jpg"},
            });
            repoMock.Setup(x => x.FindManyByAscAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(),
                It.IsAny<Expression<Func<MediaFile, object>>>(), 0, 5, x => x.DownloadDuration))
                .ReturnsAsync(new List<MediaFile>()
            {
                new(){Id = 7, Date = new(2022,02,02), Path = "C:\\", Name = "File7.jpg"},
                new(){Id = 8, Date = new(2022,02,01), Path = "C:\\", Name = "File8.jpg"},
            });


            var handler = new SearchQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<SearchDto>(result);
            var dto = (SearchDto)result;

            Assert.NotEmpty(dto.AfterRange);
            Assert.Single(dto.InRange);
            Assert.NotEmpty(dto.BeforeRange);
            var firstFile = dto.InRange.First();
            Assert.Equal(6, firstFile.Id);
            var beforeFile = dto.BeforeRange.First();
            Assert.Equal(4, beforeFile.Id);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundNotFile_Return5LatestFiles(SearchQuery request)
        {
            SetupGetMediaFilesDescAsync(new List<MediaFile>(), 1);
            SetupGetMediaFilesDescAsync(new List<MediaFile>()
            {
                new(){Id = 2, Date = new(2022,02,02), Path = "C:\\", Name = "File2.jpg"},
                new(){Id = 1, Date = new(2022,02,01), Path = "C:\\", Name = "File1.jpg"},
            }, 5);

            var handler = new SearchQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<SearchDto>(result);
            var dto = (SearchDto)result;

            Assert.Equal("Latest 5 files", dto.Message);
            var firstFile = dto.InRange.First();
            Assert.Equal(1, firstFile.Id);
        }

        private void SetupGetMediaFilesDescAsync(List<MediaFile> list, int top = 5)
        {
            repoMock.Setup(x => x.FindManyByDescAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(), It.IsAny<Expression<Func<MediaFile, object>>>(), 0, top, x => x.DownloadDuration))
                .ReturnsAsync(list);
        }

        private void SetupGetMediaFilesAscAsync(List<MediaFile> list, int top = 5)
        {
            repoMock.Setup(x => x.FindManyByAscAsync(It.IsAny<Expression<Func<MediaFile, bool>>>(), It.IsAny<Expression<Func<MediaFile, object>>>(), 0, top, x => x.DownloadDuration))
                .ReturnsAsync(list);
        }
    }
}

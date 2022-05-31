using AutoFixture.Xunit2;
using Hik.Helpers.Abstraction;
using Hik.Web.Pages;
using Hik.Web.Queries.FilePath;
using Hik.Web.Queries.Search;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Tests.Pages
{
    public class SearchModelTests : ModelTestsBase
    {
        private readonly Mock<IFilesHelper> filesHelper = new(MockBehavior.Strict);

        public SearchModelTests()
            : base()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<SearchTriggersQuery>(), default(CancellationToken)))
                .ReturnsAsync(new SearchTriggersDto() { Triggers = new Dictionary<int, string>()});
        }

        [Fact]
        public async Task OnGet_EmptyTriggerId_ReturnEmptyPage()
        {
            var sut = new SearchModel(this._mediator.Object, filesHelper.Object);
            var result = await sut.OnGetAsync(null, null);

            Assert.IsType<PageResult>(result);
            Assert.Null(sut.Dto);
        }

        [Fact]
        public async Task OnGet_EmptyDate_ReturnEmptyPage()
        {
            this._mediator.Setup(x => x.Send(It.Is<SearchQuery>(x => x.JobTriggerId == 1), default(CancellationToken)))
                .ReturnsAsync(new SearchDto());

            var sut = new SearchModel(this._mediator.Object, filesHelper.Object);
            var result = await sut.OnGetAsync(1, null);

            Assert.IsType<PageResult>(result);
            Assert.NotNull(sut.Dto);
        }

        [Fact]
        public async Task OnGet_DateHasSeconds_CutSecconds()
        {
            var date = new DateTime(2022, 02, 24, 04, 00, 59, 123);
            var expectedDate = new DateTime(2022, 02, 24, 04, 0, 0);
            this._mediator.Setup(x => x.Send(It.Is<SearchQuery>(x => x.JobTriggerId == 1 && x.DateTime == expectedDate), default(CancellationToken)))
                .ReturnsAsync(new SearchDto());

            var sut = new SearchModel(this._mediator.Object, filesHelper.Object);
            var result = await sut.OnGetAsync(1, date);

            Assert.IsType<PageResult>(result);
            Assert.NotNull(sut.Dto);
        }

        [Fact]
        public async Task OnGetDownloadFile_FileNotFound_Return404()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<FilePathQuery>(), default(CancellationToken)))
                .ReturnsAsync(default(FilePathDto));

            var sut = new SearchModel(this._mediator.Object, filesHelper.Object);
            var result = await sut.OnGetDownloadFile(1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task OnGetDownloadFile_FileNotExist_Return404()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<FilePathQuery>(), default(CancellationToken)))
                .ReturnsAsync(new FilePathDto() { Id = 1, Path = "C:\\"});
            filesHelper.Setup(x => x.FileExists("C:\\"))
                .Returns(false);

            var sut = new SearchModel(this._mediator.Object, filesHelper.Object);
            var result = await sut.OnGetDownloadFile(1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Theory]
        [AutoData]
        public async Task OnGetDownloadFile_FileExist_ReturnFile(byte[] fileBytes)
        {
            this._mediator.Setup(x => x.Send(It.IsAny<FilePathQuery>(), default(CancellationToken)))
                .ReturnsAsync(new FilePathDto() { Id = 1, Path = "C:\\"});
            filesHelper.Setup(x => x.FileExists("C:\\"))
                .Returns(true);
            filesHelper.Setup(x => x.ReadAllBytesAsync("C:\\"))
                .ReturnsAsync(fileBytes);
            filesHelper.Setup(x => x.GetFileName("C:\\"))
                .Returns("FileName");

            var sut = new SearchModel(this._mediator.Object, filesHelper.Object);
            var result = await sut.OnGetDownloadFile(1);

            Assert.IsType<FileContentResult>(result);
            var file = (FileContentResult)result;
            Assert.Equal("application/octet-stream", file.ContentType);
            Assert.Equal("FileName", file.FileDownloadName);
            Assert.Equal(fileBytes, file.FileContents);
        }

        [Fact]
        public async Task OnGetStreamFile_FileNotFound_Return404()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<FilePathQuery>(), default(CancellationToken)))
                .ReturnsAsync(default(FilePathDto));

            var sut = new SearchModel(this._mediator.Object, filesHelper.Object);
            var result = await sut.OnGetStreamFile(1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task OnGetStreamFile_FileNotExist_Return404()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<FilePathQuery>(), default(CancellationToken)))
                .ReturnsAsync(new FilePathDto() { Id = 1, Path = "C:\\"});
            filesHelper.Setup(x => x.FileExists("C:\\"))
                .Returns(false);

            var sut = new SearchModel(this._mediator.Object, filesHelper.Object);
            var result = await sut.OnGetStreamFile(1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task OnGetStreamFile_FileExist_ReturnFile()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<FilePathQuery>(), default(CancellationToken)))
                .ReturnsAsync(new FilePathDto() { Id = 1, Path = "C:\\"});
            filesHelper.Setup(x => x.FileExists("C:\\"))
                .Returns(true);
            filesHelper.Setup(x => x.ReadAsMemoryStreamAsync("C:\\"))
                .ReturnsAsync(new System.IO.MemoryStream());
            filesHelper.Setup(x => x.GetFileName("C:\\"))
                .Returns("FileName");

            var sut = new SearchModel(this._mediator.Object, filesHelper.Object);
            var result = await sut.OnGetStreamFile(1);

            Assert.IsType<FileStreamResult>(result);
            var file = (FileStreamResult)result;
            Assert.Equal("video/mp4", file.ContentType);
            Assert.True(file.EnableRangeProcessing);
            Assert.NotNull(file.FileStream);
        }
    }

}

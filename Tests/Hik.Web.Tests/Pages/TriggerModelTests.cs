using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hik.DTO.Contracts;
using Hik.Web.Commands.Cron;
using Hik.Web.Pages;
using Hik.Web.Queries.QuartzTrigger;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using Xunit;

namespace Hik.Web.Tests.Pages
{
    public class TriggerModelTests
    {
        [Fact]
        public async Task OnGetAsync_Should_Set_Trigger_Dto()
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            var model = new TriggerModel(mockMediator.Object);
            const int triggerId = 1;
            var quartzTriggerDto = new QuartzTriggerDto { Trigger = new TriggerDto { Id = triggerId } };
            mockMediator.Setup(x => x.Send(It.IsAny<QuartzTriggerQuery>(), default(CancellationToken))).ReturnsAsync(quartzTriggerDto);

            // Act
            await model.OnGetAsync(triggerId);

            // Assert
            Assert.NotNull(model.Dto);
            Assert.Equal(triggerId, model.Dto.Id);
        }

        [Fact]
        public async Task OnPostAsync_When_Model_State_Is_Invalid_Should_Return_Page()
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            var model = new TriggerModel(mockMediator.Object);
            model.ModelState.AddModelError("PropertyName", "Error Message");

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<PageResult>(result);
        }

        [Fact]
        public async Task OnPostAsync_When_Model_State_Is_valid_Should_UpsertTrigger()
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.Send(It.IsAny<UpsertTriggerCommand>(), default)).ReturnsAsync(1);

            var model = new TriggerModel(mockMediator.Object) { Dto = new TriggerDto() { Id = 1 } };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<RedirectToPageResult>(result);

            var page = result as RedirectToPageResult;

            Assert.Equal("./Scheduler", page.PageName);
            Assert.Equal("Changes saved. Take effect after Scheduler restart", page.RouteValues["msg"]);
        }

        [Fact]
        public async Task OnPostAsync_InvalidClassNamr_RedirectToSchedullerPage()
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.Send(It.IsAny<UpsertTriggerCommand>(), default)).ReturnsAsync(1);

            var model = new TriggerModel(mockMediator.Object) { Dto = new TriggerDto() { Id = 0, ClassName="InvalidClassName" } };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<RedirectToPageResult>(result);

            var page = result as RedirectToPageResult;

            Assert.Equal("./Scheduler", page.PageName);
            Assert.Equal("Invalid className", page.RouteValues["msg"]);
        }

        [Theory]
        [InlineData("GarbageCollectorJob", "GC")]
        [InlineData("FilesCollectorJob", "FilesCollector")]
        [InlineData("VideoDownloaderJob", "Camera")]
        [InlineData("PhotoDownloaderJob", "Camera")]
        public async Task OnPostAsync_CreateNewTrigger_RedirectToConfigPage(string className, string path)
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.Send(It.IsAny<UpsertTriggerCommand>(), default)).ReturnsAsync(1);

            var model = new TriggerModel(mockMediator.Object) { Dto = new TriggerDto() { Id = 0, ClassName = className } };

            // Act
            var result = await model.OnPostAsync();

            // Assert
            Assert.IsType<RedirectToPageResult>(result);

            var page = result as RedirectToPageResult;

            Assert.Equal($"./Config/{path}", page.PageName);
            Assert.Equal(1, page.RouteValues["id"]);
        }

        [Theory]
        [InlineData("GarbageCollectorJob", "{\r\n  \"RetentionPeriodDays\": -1,\r\n  \"FreeSpacePercentage\": 0.0,\r\n  \"Triggers\": [],\r\n  \"FileExtention\": null,\r\n  \"JobTimeoutMinutes\": 29,\r\n  \"DestinationFolder\": null\r\n}")]
        [InlineData("FilesCollectorJob", "{\r\n  \"FileNamePattern\": null,\r\n  \"FileNameDateTimeFormat\": null,\r\n  \"SkipLast\": 0,\r\n  \"AllowedFileExtentions\": \"*.*;\",\r\n  \"SourceFolder\": null,\r\n  \"AbnormalFilesCount\": 0,\r\n  \"JobTimeoutMinutes\": 29,\r\n  \"DestinationFolder\": null\r\n}")]
        [InlineData("VideoDownloaderJob", "{\r\n  \"ProcessingPeriodHours\": 0,\r\n  \"Camera\": null,\r\n  \"ClientType\": 0,\r\n  \"SyncTime\": true,\r\n  \"SyncTimeDeltaSeconds\": 5,\r\n  \"RemotePath\": null,\r\n  \"SaveFilesToRootFolder\": false,\r\n  \"JobTimeoutMinutes\": 29,\r\n  \"DestinationFolder\": null\r\n}")]
        [InlineData("PhotoDownloaderJob", "{\r\n  \"ProcessingPeriodHours\": 0,\r\n  \"Camera\": null,\r\n  \"ClientType\": 0,\r\n  \"SyncTime\": true,\r\n  \"SyncTimeDeltaSeconds\": 5,\r\n  \"RemotePath\": null,\r\n  \"SaveFilesToRootFolder\": false,\r\n  \"JobTimeoutMinutes\": 29,\r\n  \"DestinationFolder\": null\r\n}")]
        public void OnGetConfigJson_ClassNameExist_ReturnConfig(string className, string expexted)
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.Send(It.IsAny<UpsertTriggerCommand>(), default)).ReturnsAsync(1);

            var model = new TriggerModel(mockMediator.Object);

            // Act
            var result = model.OnGetConfigJson(className);

            // Assert
            Assert.IsType<ContentResult>(result);

            var page = result as ContentResult;

            Assert.Equal(expexted, page.Content);
        }

        [Fact]
        public void OnGetConfigJson_ClassNameNotExist_Exception()
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            mockMediator.Setup(x => x.Send(It.IsAny<UpsertTriggerCommand>(), default)).ReturnsAsync(1);

            var model = new TriggerModel(mockMediator.Object);

            // Act
            Assert.Throws<KeyNotFoundException>(() => model.OnGetConfigJson("InvalidClassName"));
        }

        [Fact]
        public void Constructor_ClassNameNotExist_Exception()
        {
            Assert.Equal(5, TriggerModel.JobTypesList.Count);
            Assert.Equal(5, TriggerModel.ConfigTypes.Count);
        }
    }
}

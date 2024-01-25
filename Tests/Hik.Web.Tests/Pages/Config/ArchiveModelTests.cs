using System.Threading.Tasks;
using Hik.DTO.Contracts;
using Hik.Web.Pages.Config;
using Hik.Web.Queries.QuartzTrigger;
using MediatR;
using Moq;
using Xunit;

namespace Hik.Web.Tests.Pages.Config
{
    public class ArchiveModelTests
    {
        [Fact]
        public async Task OnGetAsync_Should_Set_Config_When_Trigger_Exists()
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            var model = new FilesCollectorModel(mockMediator.Object);
            var triggerId = 1; // Set the ID of an existing trigger

            var quartzTriggerDto = new QuartzTriggerDto
            {
                Trigger = new TriggerDto
                {
                    Id = triggerId,
                    Config = "{\r\n  \"UnzipFiles\": false,\r\n  \"SourceFolder\": null,\r\n  \"FileNamePattern\": null,\r\n  \"FileNameDateTimeFormat\": null,\r\n  \"SkipLast\": 0,\r\n  \"AbnormalFilesCount\": 0,\r\n  \"AllowedFileExtentions\": \"*.*;\",\r\n  \"JobTimeoutMinutes\": 29,\r\n  \"DestinationFolder\": null\r\n}"
                }
            };

            mockMediator.Setup(x => x.Send(It.IsAny<QuartzTriggerQuery>(), default)).ReturnsAsync(quartzTriggerDto);

            // Act
            await model.OnGetAsync(triggerId);

            // Assert
            Assert.NotNull(model.Config);
        }

        [Fact]
        public async Task OnGetAsync_Should_Not_Set_Config_When_Trigger_Does_Not_Exist()
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            var model = new FilesCollectorModel(mockMediator.Object);
            var nonExistentTriggerId = 999; // Set an ID that doesn't exist

            mockMediator.Setup(x => x.Send(It.IsAny<QuartzTriggerQuery>(), default)).ReturnsAsync((QuartzTriggerDto)null);

            // Act
            await model.OnGetAsync(nonExistentTriggerId);

            // Assert
            Assert.Null(model.Config);
        }
    }
}

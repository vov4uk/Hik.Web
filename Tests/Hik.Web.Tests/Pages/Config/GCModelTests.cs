using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.DTO.Contracts;
using Hik.Web.Pages.Config;
using Hik.Web.Queries.QuartzTrigger;
using Hik.Web.Queries.QuartzTriggers;
using MediatR;
using Moq;
using Xunit;

namespace Hik.Web.Tests.Pages.Config
{
    public class GCModelTests
    {
        [Fact]
        public async Task OnGetAsync_Should_Set_Trigger_List_When_Triggers_Exist()
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            var model = new GCModel(mockMediator.Object);
            var triggerId = 1;
            var expectedTriggers = new List<TriggerDto>
            {
                new TriggerDto { Id = triggerId, Name = "Trigger1" },
            };

            var quartzTriggerDto = new QuartzTriggerDto
            {
                Trigger = new TriggerDto
                {
                    Id = triggerId,
                    Config = "{\r\n  \"RetentionPeriodDays\": -1,\r\n  \"FreeSpacePercentage\": 0.0,\r\n  \"Triggers\": [],\r\n  \"FileExtention\": null,\r\n  \"JobTimeoutMinutes\": 29,\r\n  \"DestinationFolder\": null\r\n}"
                }
            };

            mockMediator.Setup(x => x.Send(It.IsAny<QuartzTriggerQuery>(), default)).ReturnsAsync(quartzTriggerDto);

            var quartzTriggersDto = new QuartzTriggersDto
            {
                Triggers = expectedTriggers
            };

            // Mock the behavior of the _mediator to return expected triggers
            mockMediator.Setup(x => x.Send(It.IsAny<QuartzTriggersQuery>(), default)).ReturnsAsync(quartzTriggersDto);

            // Act
            await model.OnGetAsync(triggerId);

            // Assert
            Assert.NotNull(model.Triggers);
            Assert.Equal(expectedTriggers.Count, model.Triggers.Count);

            // Verify if the SelectListItem instances are created correctly based on the triggers received
            foreach (var trigger in expectedTriggers)
            {
                var correspondingItem = model.Triggers.FirstOrDefault(x => x.Text == trigger.Name && x.Value == trigger.Id.ToString());
                Assert.NotNull(correspondingItem);
            }
        }

        [Fact]
        public async Task OnGetAsync_Should_Not_Set_Trigger_List_When_No_Triggers_Exist()
        {
            // Arrange
            var mockMediator = new Mock<IMediator>();
            var model = new GCModel(mockMediator.Object);
            var triggerId = 1; // Set an existing trigger ID
            var emptyQuartzTriggersDto = new QuartzTriggersDto { Triggers = new List<TriggerDto>() };

            // Mock the behavior of the _mediator to return an empty list of triggers
            mockMediator.Setup(x => x.Send(It.IsAny<QuartzTriggersQuery>(), default)).ReturnsAsync(emptyQuartzTriggersDto);

            // Act
            await model.OnGetAsync(triggerId);

            // Assert
            Assert.NotNull(model.Triggers);
            Assert.Empty(model.Triggers);
        }
    }
}

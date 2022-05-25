using AutoFixture.Xunit2;
using Hik.Quartz.Contracts;
using Hik.Quartz.Services;
using Hik.Web.Queries.QuartzTriggers;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Queries.Test
{
    public class QuartzTriggersQueryHandlerTests
    {
        private readonly Mock<ICronService> cronHelper;

        public QuartzTriggersQueryHandlerTests()
        {
            this.cronHelper = new(MockBehavior.Strict);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_NoCronFound_ReturnNull(QuartzTriggersQuery request, List<CronDto> items)
        {
            cronHelper.Setup(x => x.GetAllCronsAsync())
                .ReturnsAsync(items);

            var handler = new QuartzTriggersQueryHandler( cronHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<QuartzTriggersDto>(result);
            var dto = (QuartzTriggersDto)result;
            Assert.NotEmpty(dto.Items);
        }
    }
}

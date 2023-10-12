using AutoFixture.Xunit2;
using Hik.Quartz.Contracts;
using Hik.Quartz.Services;
using Hik.Web.Queries.QuartzJob;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Queries.Test
{
    public class QuartzJobQueryHandlerTests
    {
        private readonly Mock<ICronService> cronHelper;
        private readonly Mock<IConfiguration> configuration;

        public QuartzJobQueryHandlerTests()
        {
            this.cronHelper = new(MockBehavior.Strict);
            this.configuration = new(MockBehavior.Strict);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundCron_ReturnCron(QuartzJobQuery request)
        {
            cronHelper.Setup(x => x.GetTriggerAsync(It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new CronDto() { Name = request.Name, Group = request.Group });

            var handler = new QuartzJobQueryHandler(configuration.Object, cronHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<QuartzJobDto>(result);
            var dto = (QuartzJobDto)result;
            Assert.NotNull(dto.Cron);
            Assert.Equal(request.Group, dto.Cron.Group);
            Assert.Equal(request.Name, dto.Cron.Name);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_NoCronFound_ReturnNull(QuartzJobQuery request)
        {
            cronHelper.Setup(x => x.GetTriggerAsync(It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(default(CronDto));

            var handler = new QuartzJobQueryHandler(configuration.Object, cronHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.Null(result);
        }
    }
}

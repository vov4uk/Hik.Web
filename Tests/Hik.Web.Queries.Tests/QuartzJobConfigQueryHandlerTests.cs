using AutoFixture.Xunit2;
using Hik.Helpers.Abstraction;
using Hik.Quartz.Contracts;
using Hik.Quartz.Services;
using Hik.Web.Queries.QuartzJob;
using Hik.Web.Queries.QuartzJobConfig;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Queries.Test
{
    public class QuartzJobConfigQueryHandlerTests
    {
        private readonly Mock<ICronService> cronHelper;
        private readonly Mock<IConfiguration> configuration;
        private readonly Mock<IFilesHelper> filesHelper;

        public QuartzJobConfigQueryHandlerTests()
        {
            this.cronHelper = new(MockBehavior.Strict);
            this.configuration = new(MockBehavior.Strict);
            this.filesHelper = new(MockBehavior.Strict);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FileNotExist_CreateEmptyFile(QuartzJobConfigQuery request)
        {
            cronHelper.Setup(x => x.GetCronAsync(It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new CronDto() { Name = request.Name, Group = request.Group , ConfigPath = "config.json" });
            filesHelper.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            filesHelper.Setup(x => x.WriteAllText(It.IsAny<string>(), string.Empty));

            var handler = new QuartzJobConfigQueryHandler(configuration.Object, filesHelper.Object, cronHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<QuartzJobConfigDto>(result);
            var dto = (QuartzJobConfigDto)result;
            Assert.NotNull(dto.Config);
            Assert.Equal(request.Name, dto.Config.JobName);
            Assert.Null(dto.Config.Json);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FileExist_PrettyJson(QuartzJobConfigQuery request)
        {
            var expextedJson = @"{
  ""DestinationFolder"": ""C:\\Junk"",
  ""RetentionPeriodDays"": 7
}";
            cronHelper.Setup(x => x.GetCronAsync(It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new CronDto() { Name = request.Name, Group = request.Group , ConfigPath = "config.json" });
            filesHelper.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);
            filesHelper.Setup(x => x.ReadAllText(It.IsAny<string>()))
                .ReturnsAsync("{\"DestinationFolder\":\"C:\\\\Junk\",\"RetentionPeriodDays\":7}");

            var handler = new QuartzJobConfigQueryHandler(configuration.Object, filesHelper.Object, cronHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<QuartzJobConfigDto>(result);
            var dto = (QuartzJobConfigDto)result;
            Assert.NotNull(dto.Config);
            Assert.Equal(request.Name, dto.Config.JobName);
            Assert.Equal(expextedJson, dto.Config.Json);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FileEmpty_EmptyJson(QuartzJobConfigQuery request)
        {
            cronHelper.Setup(x => x.GetCronAsync(It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new CronDto() { Name = request.Name, Group = request.Group , ConfigPath = "config.json" });
            filesHelper.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);
            filesHelper.Setup(x => x.ReadAllText(It.IsAny<string>()))
                .ReturnsAsync(default(string));

            var handler = new QuartzJobConfigQueryHandler(configuration.Object, filesHelper.Object, cronHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<QuartzJobConfigDto>(result);
            var dto = (QuartzJobConfigDto)result;
            Assert.NotNull(dto.Config);
            Assert.Equal(request.Name, dto.Config.JobName);
            Assert.Equal(string.Empty, dto.Config.Json);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_NoCronFound_ReturnNull(QuartzJobConfigQuery request)
        {
            cronHelper.Setup(x => x.GetCronAsync(It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(default(CronDto));

            var handler = new QuartzJobConfigQueryHandler(configuration.Object, filesHelper.Object, cronHelper.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.Null(result);
        }
    }
}

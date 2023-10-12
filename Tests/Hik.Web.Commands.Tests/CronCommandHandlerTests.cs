using AutoFixture.Xunit2;
using Hik.Quartz.Contracts;
using Hik.Quartz.Services;
using Hik.Web.Commands.Cron;
using MediatR;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Commands.Tests
{
    public class CronCommandHandlerTests
    {
        private readonly Mock<ICronService> cronHelper;
        private readonly Mock<IConfiguration> configuration;

        public CronCommandHandlerTests()
        {
            this.cronHelper = new(MockBehavior.Strict);
            this.configuration = new(MockBehavior.Strict);
        }

        [AutoData]
        [Theory]
        public async Task Handle_SchedulerRestarted(RestartSchedulerCommand request)
        {
            cronHelper.Setup(x => x.RestartSchedulerAsync(configuration.Object))
                .Returns(Task.CompletedTask);

            var sut = new CronCommandHandler(configuration.Object, cronHelper.Object);
            await sut.Handle(request, CancellationToken.None);

            cronHelper.Verify(x => x.RestartSchedulerAsync(configuration.Object), Times.Once);
        }


        [AutoData]
        [Theory]
        public async Task Handle_RunAsTask_StartActivity(StartActivityCommand request)
        {
            Mock<IConfigurationSection> configurationSection = new(MockBehavior.Strict);
            Mock<IConfigurationSection> connectionStringSection = new(MockBehavior.Strict);

            cronHelper.Setup(x => x.GetTriggerAsync(It.IsAny<IConfiguration>(), request.Name, request.Group))
                .ReturnsAsync(new CronDto()
                {
                    Name = request.Name,
                    Group = request.Group,
                    ClassName = "Job.Impl.DbMigrationJob, Job",
                    ConfigPath = "config.json",
                    RunAsTask = true
                });
            configuration.Setup(x => x.GetSection("DBConfiguration"))
                .Returns(configurationSection.Object);
            configurationSection.Setup(x => x.GetSection("ConnectionString"))
                .Returns(connectionStringSection.Object);
            connectionStringSection.SetupGet(x => x.Value)
                .Returns("Filename=:memory:");

            var sut = new CronCommandHandler(configuration.Object, cronHelper.Object);
            var processId = await sut.Handle(request, CancellationToken.None);

            Assert.Equal(-1, processId);
        }

        [AutoData]
        [Theory]
        public async Task Handle_NoTriigerFound_ActivityNotStarted(StartActivityCommand request)
        {
            cronHelper.Setup(x => x.GetTriggerAsync(It.IsAny<IConfiguration>(), request.Name, request.Group))
                .ReturnsAsync(default(CronDto));

            var sut = new CronCommandHandler(configuration.Object, cronHelper.Object);
            var processId = await sut.Handle(request, CancellationToken.None);

            Assert.Equal(0, processId);
        }

        [AutoData]
        [Theory]
        public async Task Handle_CronUpdated(UpdateQuartzJobCommand request)
        {
            cronHelper.Setup(x => x.UpdateTriggerAsync(configuration.Object, It.IsAny<CronDto>()))
                .Returns(Task.CompletedTask);

            var sut = new CronCommandHandler(configuration.Object, cronHelper.Object);
            await sut.Handle(request, CancellationToken.None);

            cronHelper.Verify(x => x.UpdateTriggerAsync(configuration.Object, It.IsAny<CronDto>()), Times.Once);
        }
    }
}

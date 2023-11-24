using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Quartz.Services;
using Hik.Web.Commands.Cron;
using Moq;
using Xunit;

namespace Hik.Web.Commands.Tests
{
    public class RestartSchedulerCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Restart_Scheduler()
        {
            // Arrange

            var mockCronHelper = new Mock<ICronService>();
            var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();

            var handler = new RestartSchedulerCommandHandler(
                null,
                mockCronHelper.Object,
                mockUnitOfWorkFactory.Object
            );

            var request = new RestartSchedulerCommand(); // Adjust if the command requires parameters

            var mockRepository = new Mock<IBaseRepository<JobTrigger>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            // Mocking the unit of work and repository behavior
            mockUnitOfWorkFactory
                .Setup(x => x.CreateUnitOfWork(It.IsAny<Microsoft.EntityFrameworkCore.QueryTrackingBehavior>()))
                .Returns(mockUnitOfWork.Object);

            mockUnitOfWork
                .Setup(x => x.GetRepository<JobTrigger>())
                .Returns(mockRepository.Object);

            // Mock the FindManyAsync method to return a test list of JobTriggers
            var testTriggers = new List<JobTrigger>
            {
                new JobTrigger { IsEnabled = true, TriggerKey = "Test", Group = "TestGroup", CronExpression = "*/5 * * * *", Description = "Test Description" }
            };

            mockRepository
                .Setup(x => x.FindManyAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>()))
                .ReturnsAsync(testTriggers);

            // Mock any necessary behavior for QuartzStartup and cronHelper methods

            // Act
            await handler.Handle(request, CancellationToken.None);

            mockRepository.Verify(x => x.FindManyAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>()), Times.Once);
            mockCronHelper.Verify(x => x.RestartSchedulerAsync(null), Times.Once);
        }
    }
}

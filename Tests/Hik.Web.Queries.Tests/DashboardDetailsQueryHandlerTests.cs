using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.DashboardDetails;
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
    public class DashboardDetailsQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<JobTrigger>> triggerRepoMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<DailyStatistic>> statsRepoMock = new(MockBehavior.Strict);

        public DashboardDetailsQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<JobTrigger>())
                .Returns(triggerRepoMock.Object);
            uowMock.Setup(uow => uow.GetRepository<DailyStatistic>())
                .Returns(statsRepoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundTrigger_ReturnStatistics(DashboardDetailsQuery request)
        {
            var trigger = new JobTrigger() { TriggerKey = "TriggerKey" };
            triggerRepoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>(), It.IsAny<Expression<Func<JobTrigger, object>>[]>()))
                .ReturnsAsync(trigger);
            statsRepoMock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<DailyStatistic, bool>>>()))
                .ReturnsAsync(100);
            statsRepoMock.Setup(x => x.FindManyAsync(
                It.IsAny<Expression<Func<DailyStatistic, bool>>>(),
                It.IsAny<Expression<Func<DailyStatistic, object>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<DailyStatistic, object>>[]>()))
                .ReturnsAsync(new List<DailyStatistic>()
                {
                    new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 1, Id = 11, Period = new(2022,01,01) },
                    new() { FilesCount = 11, FilesSize = 110, JobTriggerId = 1, Id = 12, Period = new(2022,01,02) },
                    new() { FilesCount = 12, FilesSize = 120, JobTriggerId = 1, Id = 13, Period = new(2022,01,03) },
                });

            var handler = new DashboardDetailsQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<DashboardDetailsDto>(result);
            var dto = (DashboardDetailsDto)result;
            Assert.Equal("TriggerKey", dto.JobTriggerName);
            Assert.Equal(100, dto.TotalItems);
            Assert.Equal(request.JobTriggerId, dto.JobTriggerId);
            Assert.NotEmpty(dto.Items);
            var day = dto.Items.First();
            Assert.Equal(10, day.FilesCount);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_NoTriggerFound_ReturnNullPath(DashboardDetailsQuery request)
        {
            triggerRepoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>(), It.IsAny<Expression<Func<JobTrigger, object>>[]>()))
                .ReturnsAsync(default(JobTrigger));

            var handler = new DashboardDetailsQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.Null(result);
        }
    }
}

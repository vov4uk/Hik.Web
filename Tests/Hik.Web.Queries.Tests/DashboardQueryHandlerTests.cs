using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.Dashboard;
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
    public class DashboardQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<JobTrigger>> triggerRepoMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<HikJob>> jobsRepoMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<DailyStatistic>> dayRepoMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<MediaFile>> fileRepoMock = new(MockBehavior.Strict);

        public DashboardQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<JobTrigger>())
                .Returns(triggerRepoMock.Object);
            uowMock.Setup(uow => uow.GetRepository<HikJob>())
                .Returns(jobsRepoMock.Object);
            uowMock.Setup(uow => uow.GetRepository<MediaFile>())
                .Returns(fileRepoMock.Object);
            uowMock.Setup(uow => uow.GetRepository<DailyStatistic>())
                .Returns(dayRepoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_GetStatistics_Return(DashboardQuery request)
        {
            var trigger1 = new JobTrigger() { Id = 1, Group = "group", TriggerKey = "Trigger1", IsEnabled = true };
            var trigger2 = new JobTrigger() { Id = 2, Group = "group", TriggerKey = "Trigger2", IsEnabled = true };
            var trigger3 = new JobTrigger() { Id = 3, Group = "group", TriggerKey = "Trigger3" };
            var triggers = new List<JobTrigger> { trigger1, trigger2, trigger3 };
            var files = new List<MediaFile>
            {
                new() { Id = 16, JobTriggerId = 1, Date = new(2022, 01, 03), Name = "File6.mp4", Size = 10, Path = "C:\\" },
                new() { Id = 23, JobTriggerId = 2, Date = new(2022, 01, 03), Name = "File3.mp4", Size = 10, Path = "C:\\" },
            };
            var days = new List<DailyStatistic>
            {
               new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 3, Id = 32, Period = new(2022, 01, 02) },
               new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 2, Id = 23, Period = new(2022, 01, 03) },
               new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 1, Id = 13, Period = new(2022,01,03) },
            };

            triggerRepoMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(triggers);
            dayRepoMock.Setup(x => x.GetLatestGroupedBy(x => x.Period == request.Day,
                    p => p.JobTriggerId))
                .ReturnsAsync(days);
            fileRepoMock.Setup(x => x.GetLatestGroupedBy(x => x.Date.Date == request.Day,
                     p => p.JobTriggerId))
                .ReturnsAsync(files);

            var jobs = new List<HikJob>()
            {
                new(){ Id = 13, JobTriggerId = 1, Finished = new(2022, 01, 03), PeriodEnd = new(2022, 01, 03)},
                new(){ Id = 22, JobTriggerId = 2, Finished = new(2022, 01, 02), PeriodEnd = new(2022, 01, 02)},
                new(){ Id = 33, JobTriggerId = 3, Finished = new(2022, 01, 03), PeriodEnd = new(2022, 01, 03)},
            };

            jobsRepoMock.Setup(x => x.GetLatestGroupedBy(It.IsAny<Expression<Func<HikJob, bool>>>(),
                It.IsAny<Expression<Func<HikJob, int>>>()))
                .ReturnsAsync(jobs);

            var handler = new DashboardQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<DashboardDto>(result);
            var dto = (DashboardDto)result;
            Assert.NotEmpty(dto.Files);
            Assert.Equal(3, dto.Files.Count);
            Assert.NotEmpty(dto.DailyStatistics);
            Assert.Equal(3, dto.DailyStatistics.Count);
            Assert.NotEmpty(dto.Triggers);
            Assert.Equal(2, dto.Triggers.Count());
        }
    }
}

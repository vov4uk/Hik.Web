using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.Dashboard;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
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
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_GetStatistics_Return(DashboardQuery request)
        {
            var trigger1 = new JobTrigger()
            {
                Id = 1, Group = "group", TriggerKey = "Trigger1",
                DailyStatistics = new()
                {
                    new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 1, Id = 11, Period = new(2022,01,01) },
                    new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 1, Id = 12, Period = new(2022,01,02) },
                    new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 1, Id = 13, Period = new(2022,01,03) },
                }
            };
            var trigger2 = new JobTrigger() { Id = 2, Group = "group", TriggerKey = "Trigger2",
                DailyStatistics = new()
                {
                    new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 2, Id = 21, Period = new(2022, 01, 01) },
                    new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 2, Id = 22, Period = new(2022, 01, 02) },
                    new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 2, Id = 23, Period = new(2022, 01, 03) },
                }
            };
            var trigger3 = new JobTrigger() { Id = 3, Group = "group", TriggerKey = "Trigger3",
                DailyStatistics = new()
                {
                    new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 3, Id = 31, Period = new(2022, 01, 01) },
                    new() { FilesCount = 10, FilesSize = 100, JobTriggerId = 3, Id = 32, Period = new(2022, 01, 02) },
                }
            };
            var triggers = new List<JobTrigger> { trigger1, trigger2, trigger3 };

            triggerRepoMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(triggers);
            triggerRepoMock.Setup(x => x.GetAll(x => x.DailyStatistics))
                .Returns(triggers.AsQueryable());

            var mediaFiles = new List<MediaFile>
            {
                new(){ Id = 11, JobTriggerId = 1, Date = new(2022, 01, 01), Name = "File1.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 12, JobTriggerId = 1, Date = new(2022, 01, 01), Name = "File2.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 13, JobTriggerId = 1, Date = new(2022, 01, 01), Name = "File3.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 14, JobTriggerId = 1, Date = new(2022, 01, 02), Name = "File4.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 15, JobTriggerId = 1, Date = new(2022, 01, 02), Name = "File5.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 16, JobTriggerId = 1, Date = new(2022, 01, 03), Name = "File6.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 21, JobTriggerId = 2, Date = new(2022, 01, 01), Name = "File1.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 22, JobTriggerId = 2, Date = new(2022, 01, 02), Name = "File2.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 23, JobTriggerId = 2, Date = new(2022, 01, 03), Name = "File3.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 30, JobTriggerId = 3, Date = new(2022, 01, 02), Name = "File1.mp4", Size = 10, Path = "C:\\"},
                new(){ Id = 31, JobTriggerId = 3, Date = new(2022, 01, 03), Name = "File2.mp4", Size = 10, Path = "C:\\"},
            };

            fileRepoMock.Setup(x => x.GetAll())
                .Returns(mediaFiles.AsQueryable());

            var jobs = new List<HikJob>()
            {
                new(){ Id = 11, JobTriggerId = 1, Finished = new(2022, 01, 01), PeriodEnd = new(2022, 01, 01)},
                new(){ Id = 12, JobTriggerId = 1, Finished = new(2022, 01, 02), PeriodEnd = new(2022, 01, 02)},
                new(){ Id = 13, JobTriggerId = 1, Finished = new(2022, 01, 03), PeriodEnd = new(2022, 01, 03)},
                new(){ Id = 14, JobTriggerId = 1, Finished = null},
                new(){ Id = 21, JobTriggerId = 2, Finished = new(2022, 01, 01), PeriodEnd = new(2022, 01, 01)},
                new(){ Id = 22, JobTriggerId = 2, Finished = new(2022, 01, 02), PeriodEnd = new(2022, 01, 02)},
                new(){ Id = 23, JobTriggerId = 2, Finished = new(2022, 01, 03), PeriodEnd = new(2022, 01, 03)},
                new(){ Id = 24, JobTriggerId = 2, Finished = null},
                new(){ Id = 31, JobTriggerId = 3, Finished = new(2022, 01, 01), PeriodEnd = new(2022, 01, 01)},
                new(){ Id = 32, JobTriggerId = 3, Finished = new(2022, 01, 02), PeriodEnd = new(2022, 01, 02)},
                new(){ Id = 33, JobTriggerId = 3, Finished = new(2022, 01, 03), PeriodEnd = new(2022, 01, 03)},
            };

            jobsRepoMock.Setup(x => x.GetAll())
                .Returns(jobs.AsQueryable());

            var handler = new DashboardQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<DashboardDto>(result);
            var dto = (DashboardDto)result;
            Assert.NotEmpty(dto.Files);
            Assert.Equal(3, dto.Files.Count);
            Assert.NotEmpty(dto.DailyStatistics);
            Assert.Equal(3, dto.DailyStatistics.Count);
            Assert.NotEmpty(dto.Triggers);
            Assert.Equal(3, dto.Triggers.Count());
        }
    }
}

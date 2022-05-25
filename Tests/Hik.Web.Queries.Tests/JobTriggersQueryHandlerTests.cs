using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.JobTriggers;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Queries.Test
{
    public class JobTriggersQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<JobTrigger>> triggerRepoMock = new(MockBehavior.Strict);

        public JobTriggersQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<JobTrigger>())
                .Returns(triggerRepoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_GetJobTriggers_Return(JobTriggersQuery request)
        {
            var trigger1 = new JobTrigger()
            {
                Id = 1,
                Group = "group",
                TriggerKey = "Trigger1",
                Jobs = new()
                {
                    new() { FilesCount = 10, JobTriggerId = 1, Id = 11, Started = new(2022, 01, 01) },
                    new() { FilesCount = 10, JobTriggerId = 1, Id = 12, Started = new(2022, 01, 02) },
                    new() { FilesCount = 10, JobTriggerId = 1, Id = 13, Started = new(2022, 01, 03) },
                }
            };
            var trigger2 = new JobTrigger()
            {
                Id = 2,
                Group = "group",
                TriggerKey = "Trigger2",
                Jobs = new()
                {
                    new() { FilesCount = 10, JobTriggerId = 2, Id = 21, Started = new(2022, 01, 01) },
                    new() { FilesCount = 10, JobTriggerId = 2, Id = 22, Started = new(2022, 01, 02) },
                    new() { FilesCount = 10, JobTriggerId = 2, Id = 23, Started = new(2022, 01, 03) },
                }
            };
            var trigger3 = new JobTrigger()
            {
                Id = 3,
                Group = "group",
                TriggerKey = "Trigger3",
                Jobs = new()
                {
                    new() { FilesCount = 10, JobTriggerId = 3, Id = 31, Started = new(2022, 01, 01) },
                    new() { FilesCount = 10, JobTriggerId = 3, Id = 32, Started = new(2022, 01, 02) },
                }
            };
            var triggers = new List<JobTrigger> { trigger1, trigger2, trigger3 };

            triggerRepoMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(triggers);
            triggerRepoMock.Setup(x => x.GetAll(x => x.Jobs))
                .Returns(triggers.AsQueryable());

            var handler = new JobTriggersQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<JobTriggersDto>(result);
            var dto = (JobTriggersDto)result;
            Assert.NotEmpty(dto.Items);
            Assert.Equal(3, dto.Items.Count);
            var lastJob = dto.Items.Last().LastJob;
            Assert.Equal(32, lastJob.Id);
        }
    }
}

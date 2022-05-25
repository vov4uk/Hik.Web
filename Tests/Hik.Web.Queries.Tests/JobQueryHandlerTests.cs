using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.Job;
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
    public class JobQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<JobTrigger>> triggerRepoMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<HikJob>> jobsRepoMock = new(MockBehavior.Strict);

        public JobQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<JobTrigger>())
                .Returns(triggerRepoMock.Object);
            uowMock.Setup(uow => uow.GetRepository<HikJob>())
                .Returns(jobsRepoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundJob_ReturnFiles(JobQuery request)
        {
            var trigger = new JobTrigger() { TriggerKey = "TriggerKey" };
            triggerRepoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>(), It.IsAny<Expression<Func<JobTrigger, object>>[]>()))
                .ReturnsAsync(trigger);
            jobsRepoMock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<HikJob, bool>>>()))
                .ReturnsAsync(100);
            jobsRepoMock.Setup(x => x.FindManyAsync(
                It.IsAny<Expression<Func<HikJob, bool>>>(),
                It.IsAny<Expression<Func<HikJob, object>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<HikJob, object>>[]>()))
                .ReturnsAsync(new List<HikJob>()
                {
                    new HikJob
                    {
                        Id = 1,
                        FilesCount = 10,
                        Success = false,
                        JobTrigger = trigger,
                        ExceptionLog = new ExceptionLog() {Message = "Something went wrong"}
                }});

            var handler = new JobQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<JobDto>(result);
            var dto = (JobDto)result;
            Assert.Equal("TriggerKey", dto.JobTriggerName);
            Assert.Equal(100, dto.TotalItems);
            Assert.Equal(request.JobTriggerId, dto.JobTriggerId);
            Assert.NotEmpty(dto.Items);
            var job = dto.Items.First();
            Assert.False(job.Success);
            Assert.Equal("Something went wrong", job.Error.Message);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_NoJobFound_ReturnNull(JobQuery request)
        {
            triggerRepoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>(), It.IsAny<Expression<Func<JobTrigger, object>>[]>()))
                .ReturnsAsync(default(JobTrigger));

            var handler = new JobQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.Null(result);
        }
    }
}

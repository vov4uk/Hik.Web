using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.JobDetails;
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
    public class JobDetailsQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<HikJob>> jobsRepoMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<MediaFile>> filesRepoMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<DownloadHistory>> downloadRepoMock = new(MockBehavior.Strict);

        public JobDetailsQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<HikJob>())
                .Returns(jobsRepoMock.Object);
            uowMock.Setup(uow => uow.GetRepository<MediaFile>())
                .Returns(filesRepoMock.Object);
            uowMock.Setup(uow => uow.GetRepository<DownloadHistory>())
                .Returns(downloadRepoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundJob_ReturnFiles(JobDetailsQuery request)
        {
            var job = new HikJob() { JobTriggerId = 1, JobTrigger = new JobTrigger { TriggerKey = "TriggerKey" } };
            jobsRepoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<HikJob, bool>>>(), It.IsAny<Expression<Func<HikJob, object>>[]>()))
                .ReturnsAsync(job);
            downloadRepoMock.Setup(x => x.CountAsync(It.IsAny<Expression<Func<DownloadHistory, bool>>>()))
                .ReturnsAsync(100);
            filesRepoMock.Setup(x => x.FindManyByDescAsync(
                It.IsAny<Expression<Func<MediaFile, bool>>>(),
                It.IsAny<Expression<Func<MediaFile, object>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<MediaFile, object>>[]>()))
                .ReturnsAsync(new List<MediaFile>()
                {
                    new(){ Id = 11, JobTriggerId = 1, Date = new(2022, 01, 01), Name = "File1.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 12, JobTriggerId = 1, Date = new(2022, 01, 01), Name = "File2.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 13, JobTriggerId = 1, Date = new(2022, 01, 01), Name = "File3.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 14, JobTriggerId = 1, Date = new(2022, 01, 02), Name = "File4.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 15, JobTriggerId = 1, Date = new(2022, 01, 02), Name = "File5.mp4", Size = 10, Path = "C:\\"},
                    new(){ Id = 16, JobTriggerId = 1, Date = new(2022, 01, 03), Name = "File6.mp4", Size = 10, Path = "C:\\"},
                });

            var handler = new JobDetailsQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<JobDetailsDto>(result);
            var dto = (JobDetailsDto)result;
            Assert.Equal("TriggerKey", dto.Job.JobTrigger);
            Assert.Equal(100, dto.TotalItems);

            Assert.NotEmpty(dto.Items);
            var file = dto.Items.First();
            Assert.Equal("File1.mp4", file.Name);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_NoJobFound_ReturnNull(JobDetailsQuery request)
        {
            jobsRepoMock.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<HikJob, bool>>>(), It.IsAny<Expression<Func<HikJob, object>>[]>()))
                .ReturnsAsync(default(HikJob));

            var handler = new JobDetailsQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.Null(result);
        }
    }
}

using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.QuartzTriggers;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Queries.Test
{
    public class QuartzTriggersQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock;
        private readonly Mock<IUnitOfWork> uowMock;
        private readonly Mock<IBaseRepository<JobTrigger>> triggerRepoMock;

        public QuartzTriggersQueryHandlerTests()
        {
            uowFactoryMock = new(MockBehavior.Strict);
            uowMock = new(MockBehavior.Strict);
            triggerRepoMock = new(MockBehavior.Strict);


            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<JobTrigger>())
                .Returns(triggerRepoMock.Object);

        }

        [Fact]
        public async Task HandleAsync_ActiveOnlyIncludeLastJob_ReturnNull()
        {
            QuartzTriggersQuery request = new QuartzTriggersQuery() { ActiveOnly = true, IncludeLastJob = true };

            triggerRepoMock.Setup(x => x.FindManyAsync(x => x.IsEnabled, x => x.LastExecutedJob))
                .ReturnsAsync(new List<JobTrigger> { new JobTrigger() });

            var handler = new QuartzTriggersQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<QuartzTriggersDto>(result);
            var dto = (QuartzTriggersDto)result;
            Assert.NotEmpty(dto.Triggers);
        }

        [Fact]
        public async Task HandleAsync_ActiveOnlyNotIncludeLastJob_ReturnNull()
        {
            QuartzTriggersQuery request = new QuartzTriggersQuery() { ActiveOnly = true, IncludeLastJob = false };

            triggerRepoMock.Setup(x => x.FindManyAsync(x => x.IsEnabled))
                .ReturnsAsync(new List<JobTrigger> { new JobTrigger() });

            var handler = new QuartzTriggersQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<QuartzTriggersDto>(result);
            var dto = (QuartzTriggersDto)result;
            Assert.NotEmpty(dto.Triggers);
        }
        
        [Fact]
        public async Task HandleAsync_NotActiveOnlyIncludeLastJob_ReturnNull()
        {
            QuartzTriggersQuery request = new QuartzTriggersQuery() { ActiveOnly = true, IncludeLastJob = true };

            triggerRepoMock.Setup(x => x.GetAll(x => x.LastExecutedJob))
                .Returns(new List<JobTrigger> { new JobTrigger() }.AsQueryable());

            var handler = new QuartzTriggersQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<QuartzTriggersDto>(result);
            var dto = (QuartzTriggersDto)result;
            Assert.NotEmpty(dto.Triggers);
        }

        [Fact]
        public async Task HandleAsync_NotActiveOnlyNotIncludeLastJob_ReturnNull()
        {
            QuartzTriggersQuery request = new QuartzTriggersQuery() { ActiveOnly = true, IncludeLastJob = false };

            triggerRepoMock.Setup(x => x.GetAllAsync())
                .ReturnsAsync(new List<JobTrigger> { new JobTrigger() });

            var handler = new QuartzTriggersQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<QuartzTriggersDto>(result);
            var dto = (QuartzTriggersDto)result;
            Assert.NotEmpty(dto.Triggers);
        }
    }
}

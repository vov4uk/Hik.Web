using AutoFixture.Xunit2;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.Search;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Queries.Test
{
    public class SearchTriggersQueryHandlerTests
    {
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock = new(MockBehavior.Strict);
        private readonly Mock<IUnitOfWork> uowMock = new(MockBehavior.Strict);
        private readonly Mock<IBaseRepository<JobTrigger>> repoMock = new(MockBehavior.Strict);

        public SearchTriggersQueryHandlerTests()
        {
            uowMock.Setup(uow => uow.Dispose());
            uowMock.Setup(uow => uow.GetRepository<JobTrigger>())
                .Returns(repoMock.Object);
            uowFactoryMock.Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(uowMock.Object);
        }

        [Theory]
        [AutoData]
        public async Task HandleAsync_FoundFile_ReturnPath(SearchTriggersQuery request)
        {
            var expected = new Dictionary<int, string>()
            {
                { 2, "TriggerKey1"},
                {1, "TriggerKey2" },
            };
            repoMock.Setup(x => x.FindManyAsync(x => x.ShowInSearch))
                .ReturnsAsync(new List<JobTrigger>()
                {
                    new(){ Id = 1, TriggerKey = "TriggerKey2"},
                    new(){ Id = 2, TriggerKey = "TriggerKey1"}
                });

            var handler = new SearchTriggersQueryHandler(uowFactoryMock.Object);
            var result = await handler.Handle(request, CancellationToken.None);

            Assert.IsType<SearchTriggersDto>(result);
            var dto = (SearchTriggersDto)result;
            Assert.Equal(expected, dto.Triggers);
        }
    }
}

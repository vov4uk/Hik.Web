using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Commands.Cron;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Hik.Web.Commands.Tests
{
    public class UpdateTriggerConfigCommandHandlerTests
    {
        [Fact]
        public async Task Handle_Should_Update_Trigger_Config()
        {
            string config = "";
            // Arrange
            var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
            var handler = new UpdateTriggerConfigCommandHandler(mockUnitOfWorkFactory.Object);

            var request = new UpdateTriggerConfigCommand
            {
                TriggerId = 1, 
                JsonConfig = "{\"key\": \"value\"}"
            };

            var mockRepository = new Mock<IBaseRepository<JobTrigger>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWorkFactory
                .Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(mockUnitOfWork.Object);

            mockUnitOfWork
                .Setup(x => x.GetRepository<JobTrigger>())
                .Returns(mockRepository.Object);

            var mockTrigger = new JobTrigger { Id = 1 };

            mockRepository
                .Setup(x => x.FindByAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>()))
                .ReturnsAsync(mockTrigger); 
            mockRepository
                .Setup(x => x.Update(It.IsAny<JobTrigger>()))
                .Callback<JobTrigger>((t)=> config = t.Config);

            // Act
            await handler.Handle(request, CancellationToken.None);

            // Assert
            mockRepository.Verify(x => x.Update(It.IsAny<JobTrigger>()), Times.Once);
            Assert.Equal(config, request.JsonConfig);
        }
    }
}

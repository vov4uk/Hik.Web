using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Web.Queries.QuartzTrigger;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Hik.Web.Queries.Tests
{
    public class QuartzTriggerQueryHandlerTests
    {
        [Fact]
        public async Task HandleAsync_When_Trigger_Exists_Should_Return_TriggerDto()
        {
            // Arrange
            var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
            var handler = new QuartzTriggerQueryHandler(mockUnitOfWorkFactory.Object);
            var requestId = 1; // Provide an ID for an existing trigger

            var mockRepository = new Mock<IBaseRepository<JobTrigger>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWorkFactory
                .Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(mockUnitOfWork.Object);

            mockUnitOfWork
                .Setup(x => x.GetRepository<JobTrigger>())
                .Returns(mockRepository.Object);

            var existingTrigger = new JobTrigger
            {
                Id = requestId,
                // Initialize other properties as needed for the trigger
            };

            mockRepository
                .Setup(x => x.FindByIdAsync(It.IsAny<int>()))
                .ReturnsAsync(existingTrigger);

            // Act
            var result = (await handler.Handle(new QuartzTriggerQuery { Id = requestId }, default)) as QuartzTriggerDto;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Trigger);
            Assert.Equal(requestId, result.Trigger.Id);
            // Add more assertions to ensure correct mapping and properties are set in the TriggerDto
        }

        [Fact]
        public async Task HandleAsync_When_Trigger_Does_Not_Exist_Should_Return_Null()
        {
            // Arrange
            var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
            var handler = new QuartzTriggerQueryHandler(mockUnitOfWorkFactory.Object);
            var nonExistentId = 999; // Provide an ID that doesn't exist

            var mockRepository = new Mock<IBaseRepository<JobTrigger>>();
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            mockUnitOfWorkFactory
                .Setup(x => x.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
                .Returns(mockUnitOfWork.Object);

            mockUnitOfWork
                .Setup(x => x.GetRepository<JobTrigger>())
                .Returns(mockRepository.Object);

            mockRepository
                .Setup(x => x.FindByAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>()))
                .ReturnsAsync((JobTrigger)null); // Simulate trigger not found

            // Act
            var result = (await handler.Handle(new QuartzTriggerQuery { Id = nonExistentId }, default)) as QuartzTriggerDto;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Trigger);
        }
    }
}

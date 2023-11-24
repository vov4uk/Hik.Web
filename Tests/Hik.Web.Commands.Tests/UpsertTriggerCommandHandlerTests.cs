using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Hik.Web.Commands.Cron;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Hik.Web.Commands.Tests
{
    public class UpsertTriggerCommandHandlerTests
    {
        [Fact]
        public async Task Handle_When_Trigger_Does_Not_Exist_Should_Add_New_Entity()
        {
            // Arrange
            var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
            var handler = new UpsertTriggerCommandHandler(mockUnitOfWorkFactory.Object);

            var request = new UpsertTriggerCommand
            {
                Trigger = new TriggerDto
                {
                    Name = "NewTrigger",
                    Group = "NewGroup",
                    // Add other necessary properties for the trigger
                }
            };

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

            JobTrigger addedTrigger = null;
            mockRepository
                .Setup(x => x.Add(It.IsAny<JobTrigger>()))
                .Callback<JobTrigger>(trigger => addedTrigger = trigger)
                .Returns(new JobTrigger());

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(addedTrigger);
            Assert.Equal(request.Trigger.Name, addedTrigger.TriggerKey);
            Assert.Equal(request.Trigger.Group, addedTrigger.Group);
            // Add more assertions based on the properties set in the handler
            // Ensure the result ID is as expected for a new entity
            Assert.Equal(addedTrigger.Id, result);
        }

        [Fact]
        public async Task Handle_When_Trigger_Exists_Should_Update_Entity()
        {
            // Arrange
            var mockUnitOfWorkFactory = new Mock<IUnitOfWorkFactory>();
            var handler = new UpsertTriggerCommandHandler(mockUnitOfWorkFactory.Object);

            var existingTrigger = new TriggerDto
            {
                Id = 1,
                Name = "ExistingTrigger",
                Group = "ExistingGroup",
                // Add other necessary properties for the trigger
            };

            var request = new UpsertTriggerCommand
            {
                Trigger = existingTrigger
            };

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
                .ReturnsAsync(new JobTrigger() { Id = 1 }); // Simulate trigger found

            JobTrigger updatedTrigger = null;
            mockRepository
                .Setup(x => x.Update(It.IsAny<JobTrigger>()))
                .Callback<JobTrigger>(trigger => updatedTrigger = trigger);

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            Assert.NotNull(updatedTrigger);
            Assert.Equal(request.Trigger.Id, updatedTrigger.Id);
            Assert.Equal(updatedTrigger.Id, result);
        }
    }
}

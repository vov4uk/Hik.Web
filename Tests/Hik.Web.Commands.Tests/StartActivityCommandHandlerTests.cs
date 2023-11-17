using System.Threading;
using System.Threading.Tasks;
using Hik.Web.Commands.Cron;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Hik.Web.Commands.Tests
{
    public class StartActivityCommandHandlerTests
    {
        [Fact]
        public void Handle_Should_Start_Activity()
        {
            // Arrange
            var mockConfiguration = new Mock<IConfiguration>();
            var handler = new StartActivityCommandHandler(mockConfiguration.Object);
            var command = new StartActivityCommand
            {
                Group = "TestGroup",
                Name = "TestActivity",
                Environment = "TestEnvironment",
                WorkingDirectory = "/test/directory"
            };

            // Mocking the GetSection method to return a test configuration
            var mockSection = new Mock<IConfigurationSection>();
            mockSection.SetupGet(x => x.Value).Returns("{\r\n    \"ConnectionString\": \"Filename=C:\\\\Code\\\\HikDatabase1.db;Pooling=True;\",\r\n    \"CommandTimeout\": 30\r\n  }");
            mockConfiguration.Setup(x => x.GetSection("DBConfiguration")).Returns(mockSection.Object);

            // Act
            var task = handler.Handle(command, CancellationToken.None);

            Assert.Equal(Task.CompletedTask, task);
        }
    }
}

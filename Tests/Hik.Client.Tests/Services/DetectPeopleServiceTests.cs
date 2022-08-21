using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Hik.Client.Abstraction;
using Hik.Client.Service;
using Hik.DTO.Config;
using Hik.DTO.Message;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hik.Client.Tests.Services
{
    public class DetectPeopleServiceTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<ILogger> loggerMock;
        private Mock<IRabbitMQFactory> rabbitMock;
        private Mock<IRabbitMQSender> senderMock;
        private readonly Fixture fixture;

        public DetectPeopleServiceTests()
        {
            this.directoryMock = new (MockBehavior.Strict);
            this.filesMock = new (MockBehavior.Strict);
            this.fixture = new ();
            this.loggerMock = new ();
        }

        [Fact]
        public async void ExecuteAsync_EmptyConfig_ExceptionThrown()
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(false);

            var service = CreateService();

            var result = await service.ExecuteAsync(default, default, default);
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid config", result.Error);
        }

        [Fact]
        public async void ExecuteAsync_DestinationNotExist_ExceptionThrown()
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(false);

            var service = CreateService();

            var result = await service.ExecuteAsync(new BaseConfig() { DestinationFolder = "C:\\"}, default, default);
            Assert.False(result.IsSuccess);
            Assert.Equal("DestinationFolder doesn't exist: C:\\", result.Error);
        }

        [Fact]
        public async Task ExecuteAsync_NoFilesFound_NothingToDo()
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(new List<string>());

            var service = CreateService();
            await service.ExecuteAsync(fixture.Create<DetectPeopleConfig>(), default, default);
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_FilesFound_SendMsg()
        {
            this.rabbitMock = new(MockBehavior.Strict);
            this.senderMock = new(MockBehavior.Strict);
            rabbitMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(senderMock.Object);
            senderMock.Setup(x => x.Dispose());

            senderMock.Setup(x => x.Sent(It.IsAny<DetectPeopleMessage>()));

            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), new[] { ".jpg" }))
                .Returns(new List<string>() { "C:\\img.jpg" });

            filesMock.Setup(x => x.GetFileName("C:\\img.jpg"))
                .Returns(string.Empty);
            filesMock.Setup(x => x.CombinePath(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(string.Empty);

            var service = CreateService();
            var result = await service.ExecuteAsync(fixture.Create<DetectPeopleConfig>(), default, default);
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), new[] { ".jpg" }), Times.Once);
            senderMock.Verify(x => x.Sent(It.IsAny<DetectPeopleMessage>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
        }

        private DetectPeopleService CreateService()
        {
            return new DetectPeopleService(this.directoryMock.Object, this.filesMock.Object, this.rabbitMock?.Object, loggerMock.Object);
        }
    }
}

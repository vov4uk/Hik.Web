using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using Hik.Client.Abstraction;
using Hik.Client.Service;
using Hik.DTO.Config;
using Hik.Helpers;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hik.Client.Tests.Services
{
    public class FtpUploaderServiceTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Mock<IUploaderClient> ftpMock;

        public FtpUploaderServiceTests()
        {
            this.directoryMock = new(MockBehavior.Strict);
            this.filesMock = new(MockBehavior.Strict);
            this.ftpMock = new(MockBehavior.Strict);
            this.loggerMock = new();
        }

        [Fact]
        public void ExecuteAsync_EmptyConfig_ExceptionThrown()
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(false);

            var service = CreateService();

            Assert.ThrowsAsync<NullReferenceException>( async() => await service.ExecuteAsync(default, default, default));            
        }

        [Theory]
        [AutoData]
        public async Task ExecuteAsync_NoFilesFound_NothingToDo(FtpUploaderConfig config)
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(new List<string>());

            var service = CreateService();
            var result = await service.ExecuteAsync(config, default, default);
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value);
        }

        [Theory]
        [AutoData]
        public async Task ExecuteAsync_FilesFound_UploadedToFtp(FtpUploaderConfig config)
        {
            config.SkipLast = 0;
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(new List<string>() { "c:\\file1.jpg", "c:\\file2.jpg" });
            filesMock.Setup(x => x.ZipFile(It.IsAny<string>())).Returns(string.Empty);
            filesMock.Setup(x => x.DeleteFile(It.IsAny<string>()));
            filesMock.Setup(x => x.GetFileName(It.IsAny<string>())).Returns("file");
            filesMock.Setup(x => x.FileSize(It.IsAny<string>())).Returns(0);
            filesMock.Setup(x => x.GetExtension(It.IsAny<string>())).Returns(".png");
            ftpMock.As<IClientBase>().Setup(x => x.InitializeClient());
            ftpMock.As<IClientBase>().Setup(x => x.Login()).Returns(true);
            ftpMock.As<IClientBase>().Setup(x => x.ForceExit());
            ftpMock.Setup(x => x.UploadFilesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var service = CreateService();
            var result = await service.ExecuteAsync(config, default, default);
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Exactly(2));
            Assert.True(result.IsSuccess);
            Assert.NotEmpty(result.Value);
           
            Assert.Equal("file", result.Value.First().Name);
            Assert.Equal("c:\\file1.jpg", result.Value.First().Path);
        }

        [Theory]
        [AutoData]
        public async Task ExecuteAsync_InvalidCredentials_Failure(FtpUploaderConfig config)
        {
            config.SkipLast = 0;
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(new List<string>() { "c:\\file1.jpg", "c:\\file2.jpg" });
            filesMock.Setup(x => x.ZipFile(It.IsAny<string>())).Returns(string.Empty);
            filesMock.Setup(x => x.DeleteFile(It.IsAny<string>()));
            filesMock.Setup(x => x.GetFileName(It.IsAny<string>())).Returns("file");
            filesMock.Setup(x => x.GetExtension(It.IsAny<string>())).Returns(".png");
            filesMock.Setup(x => x.FileSize(It.IsAny<string>())).Returns(0);
            ftpMock.As<IClientBase>().Setup(x => x.InitializeClient());
            ftpMock.As<IClientBase>().Setup(x => x.Login()).Returns(false);
            ftpMock.As<IClientBase>().Setup(x => x.ForceExit());

            var service = CreateService();
            var result = await service.ExecuteAsync(config, default, default);
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Exactly(2));
            Assert.False(result.IsSuccess);
          
            Assert.Equal("Unable to login to FTP", result.Error);
        }

        private FtpUploaderService CreateService() => new(this.directoryMock.Object, this.filesMock.Object, ftpMock.Object, loggerMock.Object);
    }
}

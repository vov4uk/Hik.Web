using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoFixture;
using Hik.Client.Abstraction;
using Hik.Client.Service;
using Hik.DTO.Config;
using Moq;
using Xunit;

namespace Hik.Client.Services.Tests
{
    public class CleanUpServiceTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Fixture fixture;

        public CleanUpServiceTests()
        {
            this.directoryMock = new Mock<IDirectoryHelper>(MockBehavior.Strict);
            this.filesMock = new Mock<IFilesHelper>(MockBehavior.Strict);
            this.fixture = new Fixture();
        }

        [Fact]
        public void ExecuteAsync_EmptyConfig_ExceptionThrown()
        {
            bool success = true;

            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>())).Returns(true);

            var service = CreateService();
            service.ExceptionFired += (object sender, Hik.Client.Events.ExceptionEventArgs e) =>
            {
                success = false;
            };

            Assert.ThrowsAsync<NullReferenceException>(() => service.ExecuteAsync(default, default(DateTime), default(DateTime)));
            Assert.False(success);
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionHappened_ExceptionHandled()
        {
            bool isOperationCanceledException = false;

            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>())).Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>())).Returns(new List<string> { "Hello World!" });
            this.directoryMock.Setup(x => x.GetTotalSpace(It.IsAny<string>())).Throws<OperationCanceledException>();

            var config = fixture.Create<CleanupConfig>();
            var service = CreateService();
            service.ExceptionFired += (object sender, Hik.Client.Events.ExceptionEventArgs e) =>
            {
                isOperationCanceledException = e.Exception is OperationCanceledException;
            };

            await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            Assert.True(isOperationCanceledException);
        }
        
        [Fact]
        public async Task ExecuteAsync_EnoughtFreeSpace_NothingToDelete()
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>())).Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>())).Returns(new List<string>());
            this.directoryMock.Setup(x => x.GetTotalSpace(It.IsAny<string>())).Returns(100);
            this.directoryMock.Setup(x => x.GetTotalFreeSpace(It.IsAny<string>())).Returns(11);
            this.directoryMock.Setup(x => x.DeleteEmptyDirs(It.IsAny<string>()));
            this.filesMock.Setup(x => x.DeleteFile(It.IsAny<string>()));

            var config = fixture.Build<CleanupConfig>()
                .With(x => x.FreeSpacePercentage, 10)
                .Create();
            var service = CreateService();

            var result = await service.ExecuteAsync(config, default(DateTime), default(DateTime));

            Assert.Empty(result);
            this.filesMock.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Never);
            this.directoryMock.Verify(x => x.DeleteEmptyDirs(It.IsAny<string>()), Times.Once);
        }
        
        [Fact]
        public async Task ExecuteAsync_NotEnoughtFreeSpace_DeleteBatchOfFiles()
        {
            var files = fixture.CreateMany<string>(20).ToList();
            int batchSize = 10;
            int fileSize = 100;

            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>())).Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>())).Returns(files);
            this.directoryMock.Setup(x => x.GetTotalSpace(It.IsAny<string>())).Returns(100);
            this.directoryMock.SetupSequence(x => x.GetTotalFreeSpace(It.IsAny<string>()))
                .Returns(9)
                .Returns(11);
            this.directoryMock.Setup(x => x.DeleteEmptyDirs(It.IsAny<string>()));
            this.filesMock.Setup(x => x.DeleteFile(It.IsAny<string>()));
            this.filesMock.Setup(x => x.GetCreationDate(It.IsAny<string>())).Returns(DateTime.Now.Date);
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>())).Returns(fileSize);
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

            var config = fixture.Build<CleanupConfig>()
                .With(x => x.FreeSpacePercentage, 10)
                .With(x => x.BatchSize, batchSize)
                .With(x => x.DestinationFolder, string.Empty)
                .Create();
            var service = CreateService();

            var result = await service.ExecuteAsync(config, default(DateTime), default(DateTime));

            Assert.Equal(result.Count, batchSize);
            this.filesMock.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Exactly(batchSize));
            this.directoryMock.Verify(x => x.DeleteEmptyDirs(It.IsAny<string>()), Times.Once);
        }
        
        [Fact]
        public async Task ExecuteAsync_FilesNotExist_NothingDeleted()
        {
            var files = fixture.CreateMany<string>(20).ToList();
            int batchSize = 10;
            int fileSize = 100;

            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>())).Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>())).Returns(files);
            this.directoryMock.Setup(x => x.GetTotalSpace(It.IsAny<string>())).Returns(100);
            this.directoryMock.Setup(x => x.GetTotalFreeSpace(It.IsAny<string>()))
                .Returns(9);
            this.directoryMock.Setup(x => x.DeleteEmptyDirs(It.IsAny<string>()));
            this.filesMock.Setup(x => x.DeleteFile(It.IsAny<string>()));
            this.filesMock.Setup(x => x.GetCreationDate(It.IsAny<string>())).Returns(DateTime.Now.Date);
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>())).Returns(fileSize);
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

            var config = fixture.Build<CleanupConfig>()
                .With(x => x.FreeSpacePercentage, 10)
                .With(x => x.BatchSize, batchSize)
                .With(x => x.DestinationFolder, string.Empty)
                .Create();
            var service = CreateService();

            var result = await service.ExecuteAsync(config, default(DateTime), default(DateTime));

            Assert.Empty(result);
            this.filesMock.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Never);
            this.directoryMock.Verify(x => x.DeleteEmptyDirs(It.IsAny<string>()), Times.Once);
        }

        private CleanUpService CreateService()
        {
            return new CleanUpService(this.directoryMock.Object, this.filesMock.Object);
        }
    }
}

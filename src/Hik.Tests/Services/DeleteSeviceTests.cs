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
    public class DeleteSeviceTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Fixture fixture;

        public DeleteSeviceTests()
        {
            this.directoryMock = new Mock<IDirectoryHelper>(MockBehavior.Strict);
            this.filesMock = new Mock<IFilesHelper>(MockBehavior.Strict);
            this.fixture = new Fixture();
        }

        [Fact]
        public void ExecuteAsync_EmptyConfig_ExceptionThrown()
        {
            bool success = true;

            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

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

            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>())).Throws<OperationCanceledException>();

            var config = fixture.Create<CameraConfig>();
            var service = CreateService();
            service.ExceptionFired += (object sender, Hik.Client.Events.ExceptionEventArgs e) =>
            {
                isOperationCanceledException = e.Exception is OperationCanceledException;
            };

            await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            Assert.True(isOperationCanceledException);
        }   
        
        [Fact]
        public async Task ExecuteAsync_Found5Files_Delete2FilesOlderThanDate()
        {
            var files = fixture.CreateMany<string>(5).ToList();
            int expectedFilesToDelete = 2;
            int fileSize = 100;
            DateTime creationDate = new DateTime(2020, 01, 31, 1, 1, 1);
            DateTime newerCreationDate = new DateTime(2020, 02, 06, 1, 1, 1);

            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>())).Returns(files);

            this.directoryMock.Setup(x => x.DeleteEmptyFolders(It.IsAny<string>()));
            this.filesMock.Setup(x => x.DeleteFile(It.IsAny<string>()));
            this.filesMock.SetupSequence(x => x.GetCreationDate(It.IsAny<string>()))
                .Returns(creationDate)
                .Returns(creationDate)
                .Returns(newerCreationDate)
                .Returns(newerCreationDate)
                .Returns(newerCreationDate);
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>())).Returns(fileSize);
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

            var config = fixture.Build<BaseConfig>()
                .With(x => x.DestinationFolder, string.Empty)
                .Create();
            var service = CreateService();

            var result = await service.ExecuteAsync(config, default(DateTime), new DateTime(2020, 02, 01));

            Assert.Equal(result.Count, expectedFilesToDelete);
            this.filesMock.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Exactly(expectedFilesToDelete));
            this.directoryMock.Verify(x => x.DeleteEmptyFolders(It.IsAny<string>()), Times.Once);
        }     
        
        [Theory]
        [InlineData("C:\\Video\\2021-02\\24\\21\\20210224_210928.jpg", "C:\\Video", "\\2021-02\\24\\21\\20210224_210928.jpg")]
        [InlineData("C:\\2021-02\\24\\21\\20210224_210928_60.mp4", "C:\\", "2021-02\\24\\21\\20210224_210928_60.mp4")]
        public async Task ExecuteAsync_Delete1File_ResultHasProperNames(string fileName, string destination, string expected)
        {
            int expectedFilesToDelete = 1;
            int fileSize = 100;
            DateTime creationDate = new DateTime(2020, 01, 31);

            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>())).Returns(new List<string> { fileName });

            this.directoryMock.Setup(x => x.DeleteEmptyFolders(It.IsAny<string>()));
            this.filesMock.Setup(x => x.DeleteFile(It.IsAny<string>()));
            this.filesMock.Setup(x => x.GetCreationDate(It.IsAny<string>())).Returns(creationDate);
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>())).Returns(fileSize);
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

            var config = fixture.Build<BaseConfig>()
                .With(x => x.DestinationFolder, destination)
                .Create();
            var service = CreateService();

            var result = await service.ExecuteAsync(config, default(DateTime), new DateTime(2020, 02, 01));

            Assert.Equal(result.Count, expectedFilesToDelete);
            var actualFile = result.FirstOrDefault();
            Assert.Equal(expected, actualFile.Name);
            Assert.Equal(creationDate, actualFile.Date);
            this.filesMock.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Exactly(expectedFilesToDelete));
            this.directoryMock.Verify(x => x.DeleteEmptyFolders(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_FilesNotExist_NothingDeleted()
        {
            var files = fixture.CreateMany<string>(20).ToList();
            int batchSize = 10;
            int fileSize = 100;

            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>())).Returns(files);
            this.directoryMock.Setup(x => x.GetTotalSpace(It.IsAny<string>())).Returns(100);
            this.directoryMock.Setup(x => x.GetTotalFreeSpace(It.IsAny<string>()))
                .Returns(9);
            this.directoryMock.Setup(x => x.DeleteEmptyFolders(It.IsAny<string>()));
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
            this.directoryMock.Verify(x => x.DeleteEmptyFolders(It.IsAny<string>()), Times.Once);
        }


        private DeleteSevice CreateService()
        {
            return new DeleteSevice(this.directoryMock.Object, this.filesMock.Object);
        }
    }
}

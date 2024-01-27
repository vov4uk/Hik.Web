using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Hik.Client.Service;
using Hik.DTO.Config;
using Hik.Helpers.Abstraction;
using Serilog;
using Moq;
using Xunit;

namespace Hik.Client.Tests.Services
{
    public class ImagesCollectorServiceTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Fixture fixture;

        public ImagesCollectorServiceTests()
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

            var service = CreateArchiveService();

            var result = await service.ExecuteAsync(default, default(DateTime), default(DateTime));
            Assert.False(result.IsSuccess);
            Assert.Equal("Invalid config", result.Error);
        }

        [Fact]
        public async Task ExecuteAsync_NoFilesFound_NothingToDo()
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(new List<string>());

            var service = CreateArchiveService();
            await service.ExecuteAsync(fixture.Create<ImagesCollectorConfig>(), default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_FoundOneFileSkipOneFile_NothingToDo()
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);

            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(new List<string> { "File" });

            var service = CreateArchiveService();
            var config = fixture.Create<ImagesCollectorConfig>();
            await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }

        [Theory]
        [InlineData("20240127133226000_2648737_02957_собака=1;людина=2.jpg", "собака=1;людина=2", "20240127_133226000", 2957, 2648737, @"C:\2024-01\27\13\20240127_133226000.jpg")]
        [InlineData("20240127134138000_2649287_02472_.jpg", "", "20240127_134138000", 2472, 2649287, "C:\\2024-01\\27\\13\\20240127_134138000.jpg")]
        public async Task ExecuteAsync_FilesFound_ProperFilesStored(
            string sourceFileName,
            string objects,
            string newName,
            int downloadDuration,
            int eventName,
            string targetFile)
        {
            var config = new ImagesCollectorConfig
            {
                DestinationFolder = "C:\\",
                SourceFolder = "E:\\",
            };

            this.directoryMock.Setup(x => x.EnumerateFiles(config.SourceFolder, new[] { ".jpg" } ))
                .Returns(new List<string> { sourceFileName });
            this.directoryMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns((string arg) => Path.GetFileNameWithoutExtension(arg));

            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns((string[] arg) => Path.Combine(arg));

            this.filesMock.Setup(x => x.GetDirectoryName(targetFile))
                .Returns(string.Empty);
            this.filesMock.Setup(x => x.FileSize(targetFile))
                .Returns(1024);
            this.filesMock.Setup(x => x.RenameFile(It.IsAny<string>(), It.IsAny<string>()));

            var service = CreateArchiveService();

            var result = await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            var actual = result.Value.First();
            Assert.Equal(1024, actual.Size);
            Assert.Equal(newName, actual.Name);
            Assert.Equal(targetFile, actual.Path);
            Assert.Equal(downloadDuration, actual.DownloadDuration);
            Assert.Equal(eventName, actual.EventId);
            Assert.Equal(objects, actual.Objects);
            //Assert.Equal(DateTime.ParseExact(date, "fileNameDateTimeFormat", null), actual.Date);
        }

        [Theory]
        [InlineData("192.168.0.65_01_19700224210928654_MOTION_DETECTION.jpg", "00010101_000000000", @"C:\0001-01\01\00\00010101_000000000.jpg")]
        public async Task ExecuteAsync_FoundFileNamesCantBeParsed_ProperFilesStored(
            string sourceFileName,
            string targetFileName,
            string targetFile)
        {
            var config = new ImagesCollectorConfig
            {
                DestinationFolder = "C:\\",
                SourceFolder = "E:\\",
            };

            this.directoryMock.Setup(x => x.EnumerateFiles(config.SourceFolder, It.IsAny<string[]>()))
                .Returns(new List<string> { sourceFileName });
            this.filesMock.Setup(x => x.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns((string arg) => Path.GetFileNameWithoutExtension(arg));
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns((string[] arg) => Path.Combine(arg));
            this.directoryMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.RenameFile(sourceFileName, targetFile));
            this.filesMock.Setup(x => x.FileSize(targetFile))
                .Returns(1024);

            this.filesMock.Setup(x => x.GetCreationDate(sourceFileName))
                .Returns(new DateTime());
            this.filesMock.Setup(x => x.DeleteFile(sourceFileName));
            this.filesMock.Setup(x => x.GetFileName(It.IsAny<string>()))
                .Returns(string.Empty);
            this.filesMock.Setup(x => x.GetDirectoryName(targetFile))
                .Returns(string.Empty);

            var service = CreateArchiveService();
            var result = await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            this.filesMock.Verify(x => x.GetCreationDate(sourceFileName), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            var actual = result.Value.First();
            Assert.Equal(1024, actual.Size);
            Assert.Equal(targetFileName, actual.Name);
            Assert.Equal(targetFile, actual.Path);
            Assert.NotNull(actual.EventId);
        }

        private ImagesCollectorService CreateArchiveService()
        {
            return new ImagesCollectorService(this.directoryMock.Object, this.filesMock.Object, loggerMock.Object);
        }
    }
}

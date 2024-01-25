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
    public class FilesCollectorServiceTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IVideoHelper> videoMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Fixture fixture;

        public FilesCollectorServiceTests()
        {
            this.directoryMock = new (MockBehavior.Strict);
            this.filesMock = new (MockBehavior.Strict);
            this.videoMock = new (MockBehavior.Strict);
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
            await service.ExecuteAsync(fixture.Create<FilesCollectorConfig>(), default(DateTime), default(DateTime));
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
            var config = fixture.Build<FilesCollectorConfig>()
                .With(x => x.SkipLast, 1)
                .Create();
            await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }

        [Theory]
        [InlineData("192.168.0.65_01_20210224210928654_MOTION_DETECTION.jpg", 60, "20210224210928654",
            "192.168.0.65_01_{1}_{2}", "yyyyMMddHHmmssfff", "C:\\2021-02\\24\\21\\20210224_210928_211028.jpg")]
        [InlineData("192.168.0.67_20210207230537_20210224_220227_0.mp4", 60, "20210224_220227",
            "192.168.0.67_{1}_{2}_0", "yyyyMMdd_HHmmss", "C:\\2021-02\\24\\22\\20210224_220227_220327.mp4")]
        public async Task ExecuteAsync_FilesFound_ProperFilesStored(
            string sourceFileName,
            int duration,
            string date,
            string fileNamePattern,
            string fileNameDateTimeFormat,
            string targetFile)
        {
            var config = new FilesCollectorConfig
            {
                DestinationFolder = "C:\\",
                SourceFolder = "E:\\",
                SkipLast = 0,
                FileNameDateTimeFormat = fileNameDateTimeFormat,
                FileNamePattern = fileNamePattern
            };

            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(config.SourceFolder, It.IsAny<string[]>()))
                .Returns(new List<string> { sourceFileName });
            this.filesMock.Setup(x => x.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns((string arg) => Path.GetFileNameWithoutExtension(arg));
            this.filesMock.Setup(x => x.GetExtension(It.IsAny<string>()))
                .Returns((string arg) => Path.GetExtension(arg));
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns((string[] arg) => Path.Combine(arg));
            this.directoryMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.RenameFile(sourceFileName, targetFile));
            this.filesMock.Setup(x => x.DeleteFile(sourceFileName));
            this.filesMock.Setup(x => x.FileSize(targetFile))
                .Returns(1024);
            this.filesMock.Setup(x => x.GetDirectoryName(targetFile))
                .Returns(string.Empty);
            this.filesMock.Setup(x => x.GetFileName(It.IsAny<string>()))
                .Returns(string.Empty);
            this.videoMock.Setup(x => x.GetDuration(It.IsAny<string>()))
                .ReturnsAsync(duration);

            var service = CreateArchiveService();

            var result = await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            var actual = result.Value.First();
            Assert.Equal(duration, actual.Duration);
            Assert.Equal(1024, actual.Size);
            Assert.Equal(string.Empty, actual.Name);
            Assert.Equal(targetFile, actual.Path);
            Assert.Equal(DateTime.ParseExact(date, fileNameDateTimeFormat, null), actual.Date);
        }

        [Theory]
        [InlineData("192.168.0.65_01_19700224210928654_MOTION_DETECTION.jpg", 60, "20210224210928654",
            "192.168.0.65_01_{1}_{2}", "yyyyMMddHHmmssfff", "C:\\2021-02\\24\\21\\20210224_210928_211028.jpg")]
        [InlineData("192.168.0.65_01_00010224210928654_MOTION_DETECTION.jpg", 60, "20210224210928654",
            "192.168.0.65_01_{1}_{2}", "yyyyMMddHHmmssfff", "C:\\2021-02\\24\\21\\20210224_210928_211028.jpg")]
        [InlineData("192.168.0.67_20210207230537_20210224_220227_0.mp4", 60, "20210224_220227",
            "192.168.0.65_{1}_{2}_0", "yyyyMMdd_HHmmss", "C:\\2021-02\\24\\22\\20210224_220227_220327.mp4")]
        public async Task ExecuteAsync_FoundFileNamesCantBeParsed_ProperFilesStored(
            string sourceFileName,
            int duration,
            string date,
            string fileNamePattern,
            string fileNameDateTimeFormat,
            string targetFile)
        {
            var dateTime = DateTime.ParseExact(date, fileNameDateTimeFormat, null);
            var config = new FilesCollectorConfig
            {
                DestinationFolder = "C:\\",
                SourceFolder = "E:\\",
                SkipLast = 0,
                FileNameDateTimeFormat = fileNameDateTimeFormat,
                FileNamePattern = fileNamePattern
            };

            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(config.SourceFolder, It.IsAny<string[]>()))
                .Returns(new List<string> { sourceFileName });
            this.filesMock.Setup(x => x.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Returns((string arg) => Path.GetFileNameWithoutExtension(arg));
            this.filesMock.Setup(x => x.GetExtension(It.IsAny<string>()))
                .Returns((string arg) => Path.GetExtension(arg));
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns((string[] arg) => Path.Combine(arg));
            this.directoryMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.RenameFile(sourceFileName, targetFile));
            this.filesMock.Setup(x => x.FileSize(targetFile))
                .Returns(1024);
            this.filesMock.Setup(x => x.GetCreationDate(sourceFileName))
                .Returns(dateTime);
            this.filesMock.Setup(x => x.DeleteFile(sourceFileName));
            this.filesMock.Setup(x => x.GetFileName(It.IsAny<string>()))
                .Returns(string.Empty);
            this.filesMock.Setup(x => x.GetDirectoryName(targetFile))
                .Returns(string.Empty);
            this.videoMock.Setup(x => x.GetDuration(It.IsAny<string>()))
                .ReturnsAsync(duration);

            var service = CreateArchiveService();
            var result = await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            this.filesMock.Verify(x => x.GetCreationDate(sourceFileName), Times.Once);
            Assert.True(result.IsSuccess);
            Assert.Single(result.Value);
            var actual = result.Value.First();
            Assert.Equal(duration, actual.Duration);
            Assert.Equal(1024, actual.Size);
            Assert.Equal(string.Empty, actual.Name);
            Assert.Equal(targetFile, actual.Path);
            Assert.Equal(DateTime.ParseExact(date, fileNameDateTimeFormat, null), actual.Date);
        }

        private FilesCollectorService CreateArchiveService()
        {
            return new FilesCollectorService(this.directoryMock.Object, this.filesMock.Object, this.videoMock.Object, loggerMock.Object);
        }
    }
}

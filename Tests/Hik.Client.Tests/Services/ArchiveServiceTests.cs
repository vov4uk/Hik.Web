using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoFixture;
using Hik.Client.Service;
using Hik.DTO.Config;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hik.Client.Tests.Services
{
    public class ArchiveServiceTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IVideoHelper> videoMock;
        private readonly Mock<IImageHelper> imageMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Fixture fixture;

        public ArchiveServiceTests()
        {
            this.directoryMock = new Mock<IDirectoryHelper>(MockBehavior.Strict);
            this.filesMock = new Mock<IFilesHelper>(MockBehavior.Strict);
            this.videoMock = new Mock<IVideoHelper>(MockBehavior.Strict);
            this.imageMock = new Mock<IImageHelper>(MockBehavior.Strict);
            this.fixture = new ();
            this.loggerMock = new ();
        }

        [Fact]
        public void ExecuteAsync_EmptyConfig_ExceptionThrown()
        {
            bool success = true;

            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);

            var service = CreateArchiveService();
            service.ExceptionFired += (object sender, Events.ExceptionEventArgs e) =>
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

            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(new List<string> { "Hello World!" });
            this.filesMock.Setup(x => x.GetFileNameWithoutExtension(It.IsAny<string>()))
                .Throws<OperationCanceledException>();

            var config = fixture.Build<ArchiveConfig>()
                .With(x => x.SkipLast, 0)
                .Create();
            var service = CreateArchiveService();
            service.ExceptionFired += (object sender, Events.ExceptionEventArgs e) =>
            {
                isOperationCanceledException = e.Exception is OperationCanceledException;
            };

            await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            Assert.True(isOperationCanceledException);
        }

        [Fact]
        public async Task ExecuteAsync_NoFilesFound_NothingToDo()
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);
            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(new List<string>());

            var service = CreateArchiveService();
            await service.ExecuteAsync(fixture.Create<ArchiveConfig>(), default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_FoundOneFileSkipOneFile_NothingToDo()
        {
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>()))
                .Returns(true);

            this.directoryMock.Setup(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()))
                .Returns(new List<string> { "File" });

            var service = CreateArchiveService();
            var config = fixture.Build<ArchiveConfig>().With(x => x.SkipLast, 1).Create();
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
            bool success = true;
            var config = new ArchiveConfig
            {
                DestinationFolder = "C:\\",
                SourceFolder = "E:\\",
                Alias = "test",
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
            this.imageMock.Setup(x => x.SetDate(sourceFileName, targetFile, It.IsAny<DateTime>()));
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
            service.ExceptionFired += (object sender, Events.ExceptionEventArgs e) =>
            {
                success = false;
            };
            var result = await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            Assert.True(success);
            Assert.Single(result);
            var actual = result.First();
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
            bool success = true;
            var dateTime = DateTime.ParseExact(date, fileNameDateTimeFormat, null);
            var config = new ArchiveConfig
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
            this.imageMock.Setup(x => x.SetDate(sourceFileName, targetFile, It.IsAny<DateTime>()));
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
            service.ExceptionFired += (object sender, Events.ExceptionEventArgs e) =>
            {
                success = false;
            };
            var result = await service.ExecuteAsync(config, default(DateTime), default(DateTime));
            this.directoryMock.Verify(x => x.EnumerateFiles(It.IsAny<string>(), It.IsAny<string[]>()), Times.Once);
            this.filesMock.Verify(x => x.GetCreationDate(sourceFileName), Times.Once);
            Assert.True(success);
            Assert.Single(result);
            var actual = result.First();
            Assert.Equal(duration, actual.Duration);
            Assert.Equal(1024, actual.Size);
            Assert.Equal(string.Empty, actual.Name);
            Assert.Equal(targetFile, actual.Path);
            Assert.Equal(DateTime.ParseExact(date, fileNameDateTimeFormat, null), actual.Date);
        }

        private ArchiveService CreateArchiveService()
        {
            return new ArchiveService(this.directoryMock.Object, this.filesMock.Object, this.videoMock.Object, this.imageMock.Object, loggerMock.Object);
        }
    }
}

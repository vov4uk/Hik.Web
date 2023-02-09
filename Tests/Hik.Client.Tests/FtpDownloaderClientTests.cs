using AutoFixture;
using FluentFTP;
using FluentFTP.Exceptions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Client.Tests
{
    public class FtpDownloaderClientTests
    {
        private readonly Mock<IAsyncFtpClient> ftpMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IDirectoryHelper> dirMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Fixture fixture;

        public FtpDownloaderClientTests()
        {
            this.ftpMock = new(MockBehavior.Strict);
            this.filesMock = new(MockBehavior.Strict);
            this.dirMock = new(MockBehavior.Strict);
            this.loggerMock = new();
            this.fixture = new();
        }

        [Fact]
        public void Constructor_PutEmptyConfig_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new FtpDownloaderClient(null, this.filesMock.Object, dirMock.Object, ftpMock.Object, loggerMock.Object));
        }

        [Fact]
        public void InitializeClient_CallInitializeClient_ClientInitialized()
        {
            var config = new CameraConfig { Camera = new DeviceConfig { IpAddress = "192.168.0.1", UserName = "admin", Password = "admin" } };

            var ftp = new AsyncFtpClient();
            var client = new FtpDownloaderClient(config, this.filesMock.Object, this.dirMock.Object, ftp, loggerMock.Object);
            client.InitializeClient();

            Assert.Equal(config.Camera.IpAddress, ftp.Host);
            Assert.Equal(config.Camera.UserName, ftp.Credentials.UserName);
            Assert.Equal(config.Camera.Password, ftp.Credentials.Password);
        }

        [Fact]
        public async Task GetFilesListAsync_CallWithValidParameters_ReturnMapppedFiles()
        {
            ftpMock.Setup(x => x.GetListing("/CameraName", default)).ReturnsAsync(new FtpListItem[]
            {
                new FtpListItem(){Name = "192.168.8.103_01_20220517095135158_MOTION_DETECTION", FullName = "/192.168.8.103_01_20220517095135158_MOTION_DETECTION.jpg"}
            });

            var config = new CameraConfig { Alias = "Group.CameraName", Camera = new DeviceConfig() };

            var client = new FtpDownloaderClient(config, this.filesMock.Object, this.dirMock.Object, this.ftpMock.Object, loggerMock.Object);
            var mediaFiles = await client.GetFilesListAsync(default, default);

            Assert.Single(mediaFiles);
            var firstFile = mediaFiles.First();
            Assert.Equal(1, firstFile.Duration);
            Assert.Equal("192.168.8.103_01_20220517095135158_MOTION_DETECTION", firstFile.Name);
            Assert.Equal("/192.168.8.103_01_20220517095135158_MOTION_DETECTION.jpg", firstFile.Path);
        }

        [Fact]
        public async Task DownloadFileAsync_FileAlreadyExist_ReturnFalse()
        {
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(this.fixture.Create<MediaFileDto>(), CancellationToken.None);

            Assert.False(isDownloaded);
        }

        [Fact]
        public async Task DownloadFileAsync_RemoteFileNotExist_ReturnFalse()
        {
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            this.ftpMock.Setup(x => x.FileExists(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(false);

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(this.fixture.Create<MediaFileDto>(), CancellationToken.None);

            Assert.False(isDownloaded);
        }

        [Fact]
        public async Task DownloadFileAsync_SizeMistmatch_DownloadFalse()
        {
            string randomName = "RandomName";

            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            this.ftpMock.Setup(x => x.FileExists(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(true);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(randomName);
            this.filesMock.Setup(x => x.RenameFile(randomName, It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(-1);
            this.ftpMock.Setup(x => x.DownloadFile(randomName, It.IsAny<string>(), FtpLocalExists.Overwrite, FtpVerify.None, null, CancellationToken.None))
                .ReturnsAsync(FtpStatus.Success);

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(this.fixture.Create<MediaFileDto>(), CancellationToken.None);

            Assert.False(isDownloaded);
        }

        [Fact]
        public async Task DownloadFileAsync_SizeMatch_DownloadTrue()
        {
            string randomName = "RandomName";
            var file = this.fixture.Create<MediaFileDto>();

            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            this.ftpMock.Setup(x => x.FileExists(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(true);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(randomName);
            this.filesMock.Setup(x => x.RenameFile(randomName, It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(file.Size);
            this.ftpMock.Setup(x => x.DownloadFile(randomName, It.IsAny<string>(), FtpLocalExists.Overwrite, FtpVerify.None, null, CancellationToken.None))
                .ReturnsAsync(FtpStatus.Success);
            this.ftpMock.Setup(x => x.DeleteFile(file.Path, CancellationToken.None))
                .Returns(Task.CompletedTask);

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(file, CancellationToken.None);

            Assert.True(isDownloaded);
        }

        [Fact]
        public async Task DownloadFileAsync_DeleteFailed_DownloadFalse()
        {
            string randomName = "RandomName";
            var file = this.fixture.Create<MediaFileDto>();

            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            this.ftpMock.Setup(x => x.FileExists(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(true);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(randomName);
            this.filesMock.Setup(x => x.RenameFile(randomName, It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(file.Size);
            this.ftpMock.Setup(x => x.DownloadFile(randomName, It.IsAny<string>(), FtpLocalExists.Overwrite, FtpVerify.None, null, CancellationToken.None))
                .ReturnsAsync(FtpStatus.Success);
            this.ftpMock.Setup(x => x.DeleteFile(file.Path, CancellationToken.None))
                .ThrowsAsync(new FtpException("Something went wrong"));

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(file, CancellationToken.None);

            Assert.False(isDownloaded);
        }

        private FtpDownloaderClient GetClient() =>
            new FtpDownloaderClient(this.fixture.Create<CameraConfig>(), this.filesMock.Object, this.dirMock.Object, this.ftpMock.Object, loggerMock.Object);
    }
}

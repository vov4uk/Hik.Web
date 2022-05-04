using AutoFixture;
using FluentFTP;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Client.Tests
{
    public class RSyncClientTests
    {
        private readonly Mock<IFtpClient> ftpMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IDirectoryHelper> dirMock;
        private readonly Fixture fixture;

        public RSyncClientTests()
        {
            this.ftpMock = new (MockBehavior.Strict);
            this.filesMock = new (MockBehavior.Strict);
            this.dirMock = new (MockBehavior.Strict);
            this.fixture = new ();
        }

        [Fact]
        public void Constructor_PutEmptyConfig_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new RSyncClient(null, this.filesMock.Object, dirMock.Object, ftpMock.Object));
        }

        [Fact]
        public void InitializeClient_CallInitializeClient_ClientInitialized()
        {
            var config = new CameraConfig { IpAddress = "192.168.0.1", UserName = "admin", Password = "admin" };
            var ftp = new FtpClient();
            var client = new RSyncClient(config, this.filesMock.Object, this.dirMock.Object, ftp);
            client.InitializeClient();

            Assert.Equal(config.IpAddress, ftp.Host);
            Assert.Equal(config.UserName, ftp.Credentials.UserName);
            Assert.Equal(config.Password, ftp.Credentials.Password);
        }

        [Fact]
        public async Task GetFilesListAsync_CallWithValidParameters_ReturnMapppedFiles()
        {
            ftpMock.Setup(x => x.GetListingAsync("/CameraName", default(CancellationToken))).ReturnsAsync(new FtpListItem[]
            {
                new FtpListItem(){Name = "00M00S"}
            });

            var config = new CameraConfig { Alias = "Group.CameraName" };

            var client = new RSyncClient(config, this.filesMock.Object, this.dirMock.Object, this.ftpMock.Object);
            var mediaFiles = await client.GetFilesListAsync(default(DateTime), default(DateTime));

            Assert.Single(mediaFiles);
            var firstFile = mediaFiles.First();
            Assert.Equal(1, firstFile.Duration);
            Assert.Equal("00M00S", firstFile.Name);
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
            var isDownloaded = await client.DownloadFileAsync(this.fixture.Create<MediaFileDTO>(), CancellationToken.None);

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
            this.ftpMock.Setup(x => x.FileExistsAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(false);

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(this.fixture.Create<MediaFileDTO>(), CancellationToken.None);

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
            this.ftpMock.Setup(x => x.FileExistsAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(true);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(randomName);
            this.filesMock.Setup(x => x.RenameFile(randomName, It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(-1);
            this.ftpMock.Setup(x => x.DownloadFileAsync(randomName, It.IsAny<string>(), FtpLocalExists.Overwrite, FtpVerify.None, null, CancellationToken.None))
                .ReturnsAsync(FtpStatus.Success);

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(this.fixture.Create<MediaFileDTO>(), CancellationToken.None);

            Assert.False(isDownloaded);
        }

        [Fact]
        public async Task DownloadFileAsync_SizeMatch_DownloadTrue()
        {
            string randomName = "RandomName";
            var file = this.fixture.Create<MediaFileDTO>();

            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            this.ftpMock.Setup(x => x.FileExistsAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(true);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(randomName);
            this.filesMock.Setup(x => x.RenameFile(randomName, It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(file.Size);
            this.ftpMock.Setup(x => x.DownloadFileAsync(randomName, It.IsAny<string>(), FtpLocalExists.Overwrite, FtpVerify.None, null, CancellationToken.None))
                .ReturnsAsync(FtpStatus.Success);
            this.ftpMock.Setup(x => x.DeleteFileAsync(file.Path, CancellationToken.None))
                .Returns(Task.CompletedTask);

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(file, CancellationToken.None);

            Assert.True(isDownloaded);
        }

        [Fact]
        public async Task DownloadFileAsync_DeleteFailed_DownloadFalse()
        {
            string randomName = "RandomName";
            var file = this.fixture.Create<MediaFileDTO>();

            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            this.ftpMock.Setup(x => x.FileExistsAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(true);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(randomName);
            this.filesMock.Setup(x => x.RenameFile(randomName, It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(file.Size);
            this.ftpMock.Setup(x => x.DownloadFileAsync(randomName, It.IsAny<string>(), FtpLocalExists.Overwrite, FtpVerify.None, null, CancellationToken.None))
                .ReturnsAsync(FtpStatus.Success);
            this.ftpMock.Setup(x => x.DeleteFileAsync(file.Path, CancellationToken.None))
                .ThrowsAsync(new Exception("Something went wrong"));

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(file, CancellationToken.None);

            Assert.False(isDownloaded);
        }

        private RSyncClient GetClient() =>
            new RSyncClient(this.fixture.Create<CameraConfig>(), this.filesMock.Object, this.dirMock.Object, this.ftpMock.Object);
    }
}

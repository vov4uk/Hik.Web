namespace Hik.Client.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstraction;
    using AutoFixture;
    using DTO.Config;
    using DTO.Contracts;
    using FluentFTP;
    using Moq;
    using Xunit;

    public class YiClientTests
    {
        private readonly Mock<IFtpClient> ftpMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IDirectoryHelper> dirMock;
        private readonly Fixture fixture;

        public YiClientTests()
        {
            this.ftpMock = new (MockBehavior.Strict);
            this.filesMock = new (MockBehavior.Strict);
            this.dirMock = new (MockBehavior.Strict);
            this.fixture = new ();
        }

        [Fact]
        public void Constructor_PutEmptyConfig_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new YiClient(null, this.filesMock.Object, dirMock.Object, ftpMock.Object));
        }

        #region InitializeClient
        [Fact]
        public void InitializeClient_CallInitializeClient_ClientInitialized()
        {
            var config = new CameraConfig { IpAddress = "192.168.0.1", UserName = "admin", Password = "admin" };
            var ftp = new FtpClient();
            var client = new YiClient(config, this.filesMock.Object, this.dirMock.Object, ftp);
            client.InitializeClient();

            Assert.Equal(config.IpAddress, ftp.Host);
            Assert.Equal(config.UserName, ftp.Credentials.UserName);
            Assert.Equal(config.Password, ftp.Credentials.Password);
        }

        #endregion InitializeClient

        #region Login
        [Fact]
        public void Login_CallLogin_LoginSucessfully()
        {
            this.ftpMock.Setup(x => x.Connect());
            var client = this.GetClient();
            bool loginResult = client.Login();

            this.ftpMock.Verify(x => x.Connect(), Times.Once);

            Assert.True(loginResult);
        }

        #endregion Login

        #region Dispose
        [Fact]
        public void Dispose_Using_DisconnectAndDispose()
        {
            this.ftpMock.Setup(x => x.Disconnect()).Verifiable();
            this.ftpMock.Setup(x => x.Dispose()).Verifiable();
            using (var client = this.GetClient())
            {
                // Do nothing
            }

            this.ftpMock.Verify();
        }
        
        [Fact]
        public void Dispose_NoFtpClient_NothingToDispose()
        {
            using (var client = new YiClient(this.fixture.Create<CameraConfig>(), this.filesMock.Object, this.dirMock.Object, null))
            {
                // Do nothing
            }

            this.ftpMock.Verify();
        }

        [Fact]
        public void Dispose_DisposeTwice_CalledOnlyOnce()
        {
            this.ftpMock.Setup(x => x.Disconnect());
            this.ftpMock.Setup(x => x.Dispose());
            using (var client = this.GetClient())
            {
                client.Dispose();
            }

            this.ftpMock.Verify(x => x.Disconnect(), Times.Once);
            this.ftpMock.Verify(x => x.Dispose(), Times.Once);
        }

        #endregion Dispose

        #region GetFilesListAsync

        [Fact]
        public async Task GetFilesListAsync_CallWithValidParameters_ReturnMapppedFiles()
        {
            DateTime start = default(DateTime);
            DateTime end = start.AddMinutes(2);

            var client = this.GetClient();
            var mediaFiles = await client.GetFilesListAsync(start, end);

            Assert.Single(mediaFiles);
            var firstFile = mediaFiles.First();
            Assert.Equal(60, firstFile.Duration);
            Assert.Equal("00M00S", firstFile.Name); 
        }

        [Fact]
        public async Task GetFilesListAsync_ShortPeriod_EmptyResult()
        {
            DateTime start = default(DateTime);
            DateTime end = start.AddMinutes(1);

            var client = this.GetClient();
            var mediaFiles = await client.GetFilesListAsync(start, end);

            Assert.Empty(mediaFiles);
        }

        [Fact]
        public async Task GetFilesListAsync_TimeWithSeconds_TruncateSeconds()
        {
            DateTimeOffset start = new DateTimeOffset(2008, 8, 22, 1, 5, 13, new TimeSpan(1, 30, 0));
            DateTime end = start.DateTime.AddMinutes(1);

            var client = this.GetClient();
            var mediaFiles = await client.GetFilesListAsync(start.DateTime, end);

            Assert.Single(mediaFiles);
            var firstFile = mediaFiles.First();
            Assert.Equal(new DateTime(2008, 8, 22, 1, 5, 0), firstFile.Date);
            Assert.Equal("05M00S", firstFile.Name);
        }
        #endregion GetFilesListAsync

        #region DownloadFileAsync
        [Theory]
        [InlineData(1991, 05, 31, ClientType.Yi,     "/tmp/sd/record/1991Y05M31D00H/00M00S.mp4",   "C:\\1991-05\\31\\00\\19910531_000000.mp4")]
        [InlineData(1991, 05, 31, ClientType.Yi720p, "/tmp/sd/record/1991Y05M31D00H/00M00S60.mp4", "C:\\1991-05\\31\\00\\19910531_000000.mp4")]
        [InlineData(2020, 12, 31, ClientType.Yi,     "/tmp/sd/record/2020Y12M31D00H/00M00S.mp4",   "C:\\2020-12\\31\\00\\20201231_000000.mp4")]
        [InlineData(2020, 12, 31, ClientType.Yi720p, "/tmp/sd/record/2020Y12M31D00H/00M00S60.mp4", "C:\\2020-12\\31\\00\\20201231_000000.mp4")]
        public async Task DownloadFileAsync_CallDownload_ProperFilesStored(int y, int m, int d, ClientType clientType, string remoteFilePath, string localFilePath)
        {
            var cameraConfig = new CameraConfig { ClientType = clientType, DestinationFolder = "C:\\", Alias = "test" };

            MediaFileDTO remoteFile = new MediaFileDTO { Date = new DateTimeOffset(y, m, d, 0, 0, 0, new TimeSpan(0, 0, 0)).UtcDateTime, Duration = 60, Name = "00M00S" };

            var tempName = localFilePath + ".tmp";
            var targetName = localFilePath;

            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns((string[] arg) => Path.Combine(arg));
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileSize(targetName))
                .Returns(1);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(tempName);
            this.filesMock.Setup(x => x.RenameFile(tempName, targetName));
            this.filesMock.Setup(x => x.FileExists(targetName))
                .Returns(false);
            this.ftpMock.Setup(x => x.FileExistsAsync(remoteFilePath, CancellationToken.None))
                .ReturnsAsync(true);
            this.ftpMock.Setup(x => x.DownloadFileAsync(tempName, remoteFilePath, FtpLocalExists.Overwrite, FtpVerify.None, null, CancellationToken.None))
                .ReturnsAsync(FtpStatus.Success);

            var client = new YiClient(cameraConfig, this.filesMock.Object, this.dirMock.Object, ftpMock.Object);
            var isDownloaded = await client.DownloadFileAsync(remoteFile, CancellationToken.None);

            Assert.True(isDownloaded);
            this.filesMock.Verify(x => x.CombinePath(It.IsAny<string[]>()), Times.Exactly(2));
            this.dirMock.Verify(x => x.CreateDirIfNotExist(It.IsAny<string>()), Times.Once);
            this.filesMock.Verify(x => x.FileExists(targetName), Times.Once);
            this.filesMock.Verify(x => x.RenameFile(tempName, targetName), Times.Once);
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
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.ftpMock.Setup(x => x.FileExistsAsync(It.IsAny<string>(), CancellationToken.None))
                .ReturnsAsync(false);

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(this.fixture.Create<MediaFileDTO>(), CancellationToken.None);

            Assert.False(isDownloaded);
        }

        #endregion DownloadFileAsync

        #region ForceExit
        [Fact]
        public void ForceExit_InvokeMethod_DisconnectClient()
        {
            this.ftpMock.Setup(x => x.Disconnect());
            this.ftpMock.Setup(x => x.Dispose());
            var client = this.GetClient();
            client.ForceExit();
            this.ftpMock.Verify(x => x.Disconnect(), Times.Once);
            this.ftpMock.Verify(x => x.Dispose(), Times.Once);
        }

        #endregion ForceExit

        private YiClient GetClient() =>
               new YiClient(this.fixture.Create<CameraConfig>(), this.filesMock.Object, this.dirMock.Object, this.ftpMock.Object);
    }
}

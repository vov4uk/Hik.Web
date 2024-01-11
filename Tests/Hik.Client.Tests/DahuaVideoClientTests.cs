namespace Hik.Client.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoMapper;
     using Hik.Client.Infrastructure;
    using Hik.DTO.Config;
    using Hik.DTO.Contracts;
    using Hik.Helpers.Abstraction;
    using Serilog;
    using Moq;
    using Xunit;
    using Dahua.Api.Abstractions;
    using Hik.Client.Client;
    using Dahua.Api.Data;

    public class DahuaVideoClientTests
    {
        private readonly Mock<IDahuaSDK> sdkMock;
        private readonly Mock<IDahuaApi> apiMock;
        private readonly Mock<IVideoService> videoServiceMock;
        private readonly Mock<IConfigService> configServiceMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IDirectoryHelper> dirMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Fixture fixture;
        private readonly IMapper mapper;

        public DahuaVideoClientTests()
        {
            this.videoServiceMock = new (MockBehavior.Strict);
            this.configServiceMock = new (MockBehavior.Strict);
            this.loggerMock = new ();
            this.sdkMock = new (MockBehavior.Strict);
            this.apiMock = new(MockBehavior.Strict);
            this.apiMock.SetupGet(x => x.VideoService)
                .Returns(this.videoServiceMock.Object);
            this.apiMock.SetupGet(x => x.ConfigService)
                .Returns(this.configServiceMock.Object);

            this.filesMock = new(MockBehavior.Strict);
            this.dirMock = new(MockBehavior.Strict);
            this.fixture = new();
            Action<IMapperConfigurationExpression> configureAutoMapper = x =>
            {
                x.AddProfile<HikConsoleProfile>();
            };

            var mapperConfig = new MapperConfiguration(configureAutoMapper);
            this.mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public void Constructor_PutEmptyConfig_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new DahuaVideoClient(null, this.sdkMock.Object, this.filesMock.Object, this.dirMock.Object, this.mapper, this.loggerMock.Object));
        }

        #region InitializeClient
        [Fact]
        public void InitializeClient_CallInitializeClient_ClientInitialized()
        {
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.sdkMock.Setup(x => x.Initialize());

            this.GetClient().InitializeClient();

            this.sdkMock.Verify(x => x.Initialize(), Times.Once);
        }

        #endregion InitializeClient

        #region Login
        [Fact]
        public void Login_CallLogin_LoginSuccessfully()
        {
            this.SetupLogin();
            var client = this.GetClient();
            bool loginResult = client.Login();

            this.sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.True(loginResult);
        }



        [Fact]
        public void Login_CallLoginTwice_LoginOnce()
        {
            this.SetupLogin();

            var client = this.GetClient();
            var first = client.Login();
            var second = client.Login();

            Assert.True(first);
            Assert.False(second);
            this.sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        #endregion Login

        #region Dispose
        [Fact]
        public void Dispose_CallLogin_LogoutSuccess()
        {
            this.SetupLogin();

            this.apiMock.Setup(x => x.Logout());
            this.sdkMock.Setup(x => x.Cleanup());

            bool loginResult = false;
            using (var client = this.GetClient())
            {
                loginResult = client.Login();
            }

            Assert.True(loginResult);
            this.sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.apiMock.Verify(x => x.Logout(), Times.Once);
            this.sdkMock.Verify(x => x.Cleanup(), Times.Once);
        }

        [Fact]
        public void Dispose_DoNotLogin_LogoutNotCall()
        {
            this.sdkMock.Setup(x => x.Cleanup()).Verifiable();
            using (var client = this.GetClient())
            {
                // Do nothing
            }

            this.sdkMock.Verify();
            this.apiMock.Verify(x => x.Logout(), Times.Never);
        }

        #endregion Dispose

        #region GetFilesListAsync
        [Fact]
        public async Task GetFilesListAsync_CallWithValidParameters_ReturnMappedFiles()
        {
            DateTime start = default(DateTime);
            DateTime end = start.AddSeconds(1);
            var remoteFile = fixture.Create<RemoteFile>();

            this.SetupLogin();
            this.videoServiceMock.Setup(x => x.FindFiles(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<RemoteFile>() { remoteFile });

            var client = this.GetClient();
            client.Login();
            var mediaFiles = await client.GetFilesListAsync(start, end);

            this.videoServiceMock.Verify(x => x.FindFiles(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
            Assert.Single(mediaFiles);
            var firstFile = mediaFiles.First();
            Assert.Equal(remoteFile.Name, firstFile.Name);
            Assert.Equal(remoteFile.Date, firstFile.Date);
            Assert.Equal(remoteFile.Duration, firstFile.Duration);
            Assert.Equal(remoteFile.Size, firstFile.Size);
        }

        [Fact]
        public async Task GetFilesListAsync_CallWithInvalidParameters_ThrowsException()
        {
            var client = this.GetClient();

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var date = new DateTime(1970, 1, 1);
                await client.GetFilesListAsync(date, date);
            });
        }

        #endregion GetFilesListAsync

        #region DownloadFileAsync

        [Theory]
        [InlineData(1991, 05, 31, 60, "video", "C:\\1991-05\\31\\00\\19910531_000000_000100.mp4")]
        [InlineData(2020, 12, 31, 3600, "ch000000001", "C:\\2020-12\\31\\00\\20201231_000000_010000.mp4")]
        public async Task DownloadFileAsync_CallDownload_ProperFilesStored(int y, int m, int d, int duration, string name, string fileName)
        {
            var cameraConfig = new CameraConfig { ClientType = ClientType.HikVisionVideo, DestinationFolder = "C:\\", Camera = new DTO.Config.DeviceConfig() };

            long downloadHandler = 1;
            this.SetupLogin();
            MediaFileDto remoteFile = new MediaFileDto { Date = new DateTime(y, m, d), Duration = duration, Name = name };

            var tempName = fileName + ".tmp";
            var targetName = fileName;

            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns((string[] arg) => Path.Combine(arg));
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileSize(targetName))
                .Returns(1);
            this.filesMock.Setup(x => x.RenameFile(tempName + ".mp4", targetName));
            this.filesMock.Setup(x => x.FileExists(targetName))
                .Returns(false);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(tempName);

            this.videoServiceMock.Setup(x => x.StartDownloadFile(It.IsAny<IRemoteFile>(), tempName + ".mp4"))
                .Returns(downloadHandler);
            this.videoServiceMock.Setup(x => x.GetDownloadPosition(It.IsAny<long>()))
                .Returns((false,1,1));
            this.videoServiceMock.Setup(x => x.StopDownloadFile(It.IsAny<long>()))
                .Returns(true);

            var client = new DahuaVideoClient(cameraConfig, this.sdkMock.Object, this.filesMock.Object, this.dirMock.Object, this.mapper, loggerMock.Object);
            client.Login();
            var isDownloaded = await client.DownloadFileAsync(remoteFile, CancellationToken.None);

            Assert.True(isDownloaded);
            this.filesMock.Verify(x => x.CombinePath(It.IsAny<string[]>()), Times.Exactly(2));
            this.dirMock.Verify(x => x.CreateDirIfNotExist(It.IsAny<string>()), Times.Once);
            this.filesMock.Verify(x => x.FileExists(targetName), Times.Once);
            this.filesMock.Verify(x => x.GetTempFileName(), Times.Once);
            this.filesMock.Verify(x => x.RenameFile(tempName + ".mp4", targetName), Times.Once);
            this.videoServiceMock.Verify(x => x.StartDownloadFile(It.IsAny<IRemoteFile>(), tempName + ".mp4"), Times.Once);
        }

        [Fact]
        public async Task DownloadFileAsync_FileAlreadyExist_ReturnFalse()
        {
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(string.Empty);

            var client = this.GetClient();
            var isDownloaded = await client.DownloadFileAsync(this.fixture.Create<MediaFileDto>(), CancellationToken.None);

            Assert.False(isDownloaded);
        }

        [Fact]
        public async Task DownloadFileAsync_AbnormalProgress_StopDownloadFile()
        {
            long downloadHandler = 1;
            this.SetupLogin();

            this.SetupFilesMockForDownload();

            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(string.Empty);
            this.videoServiceMock
                .Setup(x => x.StartDownloadFile(It.IsAny<IRemoteFile>(), It.IsAny<string>()))
                .Returns(downloadHandler);
            this.videoServiceMock.Setup(x => x.StopDownloadFile(downloadHandler))
                .Returns(true);
            this.videoServiceMock.Setup(x => x.GetDownloadPosition(It.IsAny<long>()))
                .Returns((false,0,0));
            this.apiMock.Setup(x => x.Logout());
            this.sdkMock.Setup(x => x.Cleanup());

            DahuaVideoClient client = null;
            using (client = this.GetClient())
            {
                client.Login();
                await client.DownloadFileAsync(this.fixture.Create<MediaFileDto>(), CancellationToken.None);
            }

            this.videoServiceMock.Verify(x => x.StopDownloadFile(downloadHandler), Times.Once);
            this.apiMock.Verify(x => x.Logout(), Times.Once);
            this.sdkMock.Verify(x => x.Cleanup(), Times.Once);
        }

        #endregion DownloadFileAsync

        #region ForceExit
        [Fact]
        public void ForceExit_FilesNotDownloading_DoNotDeleteFile()
        {
            this.SetupLogin();

            this.apiMock.Setup(x => x.Logout());
            this.sdkMock.Setup(x => x.Cleanup());

            var client = this.GetClient();
            client.Login();
            client.ForceExit();

            this.apiMock.Verify(x => x.Logout(), Times.Once);
            this.sdkMock.Verify(x => x.Cleanup(), Times.Once);
        }

        [Fact]
        public async Task ForceExit_FileIsDownloading_DoStop()
        {
            long downloadHandler = 1;
            var client = this.GetClient();
            this.SetupLogin();
            this.SetupFilesMockForDownload();

            this.videoServiceMock
                .Setup(x => x.StartDownloadFile(It.IsAny<IRemoteFile>(), It.IsAny<string>()))
                .Returns(downloadHandler);
            this.videoServiceMock.Setup(x => x.StopDownloadFile(downloadHandler))
                .Returns(true);
            this.videoServiceMock.Setup(x => x.GetDownloadPosition(It.IsAny<long>()))
                .Callback(client.ForceExit)
                .Returns((true, 1, 10));
            this.apiMock.Setup(x => x.Logout());
            this.sdkMock.Setup(x => x.Cleanup());
            this.filesMock.Setup(x => x.GetTempFileName())
                .Returns(string.Empty);


            client.Login();
            var isDownloadingStarted = await client.DownloadFileAsync(this.fixture.Create<MediaFileDto>(), CancellationToken.None);
            
            this.apiMock.Verify(x => x.Logout(), Times.Once);
            this.sdkMock.Verify(x => x.Cleanup(), Times.Once);

            Assert.True(isDownloadingStarted);
            this.videoServiceMock.Verify(x => x.StopDownloadFile(downloadHandler), Times.Once);
        }

        #endregion ForceExit

        private void SetupLogin()
        {
            this.sdkMock.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(apiMock.Object);
        }

        private void SetupFilesMockForDownload()
        {
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            this.dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(1);
            this.filesMock.Setup(x => x.RenameFile(It.IsAny<string>(), It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(false);
        }

        private DahuaVideoClient GetClient() =>
            new DahuaVideoClient(this.fixture.Create<CameraConfig>(), this.sdkMock.Object, this.filesMock.Object, this.dirMock.Object, this.mapper, loggerMock.Object);
     }
}

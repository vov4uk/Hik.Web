namespace HikConsole.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoMapper;
    using Hik.Api;
    using Hik.Api.Data;
    using Hik.Client.Abstraction;
    using Hik.Client.Infrastructure;
    using Hik.Client.Service;
    using Hik.DTO.Config;
    using Moq;
    using Xunit;

    public class HikDownloaderTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Fixture fixture;
        private readonly Mock<IHikClient> clientMock;
        private readonly Mock<IHikClientFactory> clientFactoryMock;
        private readonly IMapper mapper;

        public HikDownloaderTests()
        {
            this.directoryMock = new Mock<IDirectoryHelper>(MockBehavior.Strict);
            this.clientMock = new Mock<IHikClient>(MockBehavior.Strict);
            this.clientFactoryMock = new Mock<IHikClientFactory>(MockBehavior.Strict);

            // temp fix, till not cover photo download with UT
            this.clientMock.Setup(x => x.FindPhotosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<RemotePhotoFile>());

            this.fixture = new Fixture();
            var status = this.fixture.Build<HdInfo>().With(x => x.HdStatus, 0U).Create();
            this.clientMock.Setup(x => x.CheckHardDriveStatus()).Returns(status);

            Action<IMapperConfigurationExpression> configureAutoMapper = x =>
            {
                x.AddProfile<HikConsoleProfile>();
            };

            var mapperConfig = new MapperConfiguration(configureAutoMapper);
            this.mapper = mapperConfig.CreateMapper();
        }

        [Fact]
        public async Task DownloadAsync_EmptyConfig_NothingToDo()
        {
            bool success = true;
            this.clientFactoryMock.Setup(x => x.Create(It.IsAny<CameraConfig>())).Throws<ArgumentNullException>();

            var downloader = new HikVideoDownloaderService(this.directoryMock.Object, this.clientFactoryMock.Object, this.mapper);
            downloader.ExceptionFired += (object sender, Hik.Client.Events.ExceptionEventArgs e) =>
            {
                success = false;
            };

            await downloader.ExecuteAsync(default(CameraConfig), default(DateTime), default(DateTime));

            this.clientMock.Verify(x => x.InitializeClient(), Times.Never);
            Assert.False(success);
        }

        [Fact]
        public async Task DownloadAsync_LoginFailed_LogWarning()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.SetupDirectoryHelper();
            this.SetupClientInitialize();
            this.SetupClientDispose();
            this.clientMock.Setup(x => x.Login()).Returns(false);

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task DownloadAsync_LoginThrowsException_HandleException()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.SetupClientInitialize();
            this.clientMock.Setup(x => x.ForceExit());
            this.clientMock.Setup(x => x.Login()).Throws(new HikException("Login", 7));

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.ForceExit(), Times.Once);
        }

        [Fact]
        public async Task DownloadAsync_FindNoFiles_NothingToDownload()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();

            this.clientMock.Setup(x => x.FindVideosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Array.Empty<RemoteVideoFile>());

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.directoryMock.Verify(x => x.GetTotalFreeSpace(It.IsAny<string>()), Times.Once);
            this.directoryMock.Verify(x => x.DirSize(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DownloadAsync_FindOneFiles_StartDownload()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var file = this.fixture.Build<RemoteVideoFile>()
                .Create();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();
            this.SetupUpdateProgress();
            this.clientMock.Setup(x => x.StartVideoDownload(file)).Returns(true);
            this.clientMock.SetupGet(x => x.IsDownloading).Returns(false);

            this.clientMock.Setup(x => x.FindVideosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new RemoteVideoFile[] { file, file });

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.clientMock.Verify(x => x.StartVideoDownload(file), Times.Once);
            this.clientMock.Verify(x => x.UpdateVideoProgress(), Times.Once);
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task DownloadAsync_FindManyFiles_AllStartDownload()
        {
            int filesCount = 3;

            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var files = this.fixture.Build<RemoteVideoFile>()
                .CreateMany(filesCount)
                .ToArray();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();
            this.SetupUpdateProgress();
            this.clientMock.Setup(x => x.StartVideoDownload(It.IsAny<RemoteVideoFile>())).Returns(true);
            this.clientMock.SetupGet(x => x.IsDownloading).Returns(false);

            this.clientMock.Setup(x => x.FindVideosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(files);

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.clientMock.Verify(x => x.StartVideoDownload(It.IsAny<RemoteVideoFile>()), Times.Exactly(filesCount - 1));
            this.clientMock.Verify(x => x.UpdateVideoProgress(), Times.Exactly(filesCount - 1));
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task DownloadAsync_FindManyFiles_OnlyOneStartDownload()
        {
            int filesCount = 3;

            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var files = this.fixture.Build<RemoteVideoFile>()
                .CreateMany(filesCount)
                .ToArray();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();
            this.SetupUpdateProgress();
            this.clientMock.SetupSequence(x => x.StartVideoDownload(It.IsAny<RemoteVideoFile>()))
                .Returns(false)
                .Returns(true);

            this.clientMock.Setup(x => x.IsDownloading).Returns(false);

            this.clientMock.Setup(x => x.FindVideosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(files);

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.clientMock.Verify(x => x.StartVideoDownload(It.IsAny<RemoteVideoFile>()), Times.Exactly(filesCount - 1));
            this.clientMock.Verify(x => x.UpdateVideoProgress(), Times.Once);
            this.clientMock.VerifyGet(x => x.IsDownloading, Times.Once);
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task DownloadAsync_FindOneFiles_LongRunningDownload()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            var file = this.fixture.Build<RemoteVideoFile>()
                .Create();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();
            this.SetupUpdateProgress();
            this.clientMock.Setup(x => x.StartVideoDownload(It.IsAny<RemoteVideoFile>())).Returns(true).Verifiable();
            this.clientMock.SetupSequence(x => x.IsDownloading)
                .Returns(true)
                .Returns(true)
                .Returns(false);

            this.clientMock.Setup(x => x.FindVideosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new RemoteVideoFile[] { file, file });

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify();
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.clientMock.Verify(x => x.StartVideoDownload(file), Times.Once);
            this.clientMock.Verify(x => x.UpdateVideoProgress(), Times.Exactly(3));
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task Cancel_CancelationOnClintItitialize_ClientForceExited()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.clientMock.Setup(x => x.InitializeClient());
            this.clientMock.Setup(x => x.ForceExit());
            this.SetupClientDispose();

            // act
            var downloader = this.CreateHikDownloader();

            this.clientMock.Setup(x => x.InitializeClient()).Callback(downloader.Cancel);

            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.ForceExit(), Times.Once);
        }

        private void SetupDirectoryHelper()
        {
            this.directoryMock.Setup(x => x.GetTotalFreeSpace(It.IsAny<string>())).Returns(1024);
            this.directoryMock.Setup(x => x.DirSize(It.IsAny<string>())).Returns(1024);
        }

        private void SetupClientSuccessLogin()
        {
            this.SetupClientInitialize();
            this.clientMock.Setup(x => x.Login()).Returns(true);
        }

        private void SetupClientInitialize()
        {
            this.clientMock.Setup(x => x.InitializeClient());
        }

        private void SetupClientDispose()
        {
            this.clientMock.Setup(x => x.Dispose());
        }

        private void SetupUpdateProgress()
        {
            this.clientMock.Setup(x => x.UpdateVideoProgress());
        }

        private void VerifyStatisticWasPrinted()
        {
            this.directoryMock.Verify(x => x.GetTotalFreeSpace(It.IsAny<string>()), Times.Once);
            this.directoryMock.Verify(x => x.DirSize(It.IsAny<string>()), Times.Once);
        }

        private HikVideoDownloaderService CreateHikDownloader()
        {
            this.clientFactoryMock.Setup(x => x.Create(It.IsAny<CameraConfig>())).Returns(this.clientMock.Object);

            return new HikVideoDownloaderService(this.directoryMock.Object, this.clientFactoryMock.Object, this.mapper);
        }
    }
}

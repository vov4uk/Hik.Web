using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using AutoFixture;
using HikApi;
using HikApi.Data;
using HikConsole.Abstraction;
using HikConsole.Config;
using Moq;
using Xunit;

namespace HikConsole.Tests
{
    public class HikDownloaderTests
    {
        private readonly IContainer container;
        private readonly Mock<ILogger> loggerMock;
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Mock<IEmailHelper> emailMock;
        private readonly Fixture fixture;
        private readonly Mock<IHikClient> clientMock;

        public HikDownloaderTests()
        {
            var builder = new ContainerBuilder();

            this.loggerMock = new Mock<ILogger>();
            this.directoryMock = new Mock<IDirectoryHelper>(MockBehavior.Strict);
            this.emailMock = new Mock<IEmailHelper>(MockBehavior.Strict);
            this.clientMock = new Mock<IHikClient>(MockBehavior.Strict);

            builder.RegisterInstance<ILogger>(this.loggerMock.Object);
            builder.RegisterInstance<IHikClient>(this.clientMock.Object);
            builder.RegisterInstance<IEmailHelper>(this.emailMock.Object);
            builder.RegisterInstance<IDirectoryHelper>(this.directoryMock.Object);

            this.fixture = new Fixture();

            this.container = builder.Build();
        }

        [Fact]
        public async Task DownloadAsync_EmptyConfig_NothingToDo()
        {
            var appcConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { })
                    .Create();
            var downloader = new HikDownloader(appcConfig, this.container, 100);

            await downloader.DownloadAsync();

            this.clientMock.Verify(x => x.InitializeClient(), Times.Never);
        }

        [Fact]
        public async Task DownloadAsync_LoginFailed_LogWarning()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var appcConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();

            this.SetupClientInitialize();
            this.clientMock.Setup(x => x.Login()).Returns(false);

            // act
            var downloader = new HikDownloader(appcConfig, this.container, 100);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.loggerMock.Verify(x => x.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DownloadAsync_LoginThrowsException_HandleException()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var appcConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();

            this.SetupClientInitialize();
            this.clientMock.Setup(x => x.ForceExit());
            this.clientMock.Setup(x => x.Login()).Throws(new HikException("Login", 7));
            this.emailMock.Setup(x => x.SendEmail(appcConfig.EmailConfig, It.IsAny<string>()));

            // act
            var downloader = new HikDownloader(appcConfig, this.container, 100);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.loggerMock.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
            this.emailMock.Verify(x => x.SendEmail(appcConfig.EmailConfig, It.IsAny<string>()), Times.Once);
            this.clientMock.Verify(x => x.ForceExit(), Times.Once);
        }

        [Fact]
        public async Task DownloadAsync_FindNoFiles_NothingToDownload()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var appcConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();

            this.SetupClientSucessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientLogout();

            this.clientMock.Setup(x => x.FindAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new RemoteVideoFile[] { });

            // act
            var downloader = new HikDownloader(appcConfig, this.container, 100);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Logout(), Times.Once);
            this.directoryMock.Verify(x => x.GetTotalFreeSpace(It.IsAny<string>()), Times.Once);
            this.directoryMock.Verify(x => x.DirSize(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DownloadAsync_FindOneFiles_StartDownload()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var appcConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();
            var file = this.fixture.Build<RemoteVideoFile>()
                .Create();

            this.SetupClientSucessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientLogout();
            this.SetupUpdateProgress();
            this.clientMock.Setup(x => x.StartDownload(file)).Returns(true);
            this.clientMock.SetupGet(x => x.IsDownloading).Returns(false);

            this.clientMock.Setup(x => x.FindAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new RemoteVideoFile[] { file });

            // act
            var downloader = new HikDownloader(appcConfig, this.container, 100);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Logout(), Times.Once);
            this.clientMock.Verify(x => x.StartDownload(file), Times.Once);
            this.clientMock.Verify(x => x.UpdateProgress(), Times.Once);
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task DownloadAsync_FindManyFiles_AllStartDownload()
        {
            int filesCount = 3;

            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var appcConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();
            var files = this.fixture.Build<RemoteVideoFile>()
                .CreateMany(filesCount)
                .ToArray();

            this.SetupClientSucessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientLogout();
            this.SetupUpdateProgress();
            this.clientMock.Setup(x => x.StartDownload(It.IsAny<RemoteVideoFile>())).Returns(true);
            this.clientMock.SetupGet(x => x.IsDownloading).Returns(false);

            this.clientMock.Setup(x => x.FindAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(files);

            // act
            var downloader = new HikDownloader(appcConfig, this.container, 100);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Logout(), Times.Once);
            this.clientMock.Verify(x => x.StartDownload(It.IsAny<RemoteVideoFile>()), Times.Exactly(filesCount));
            this.clientMock.Verify(x => x.UpdateProgress(), Times.Exactly(filesCount));
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task DownloadAsync_FindManyFiles_OnlyOneStartDownload()
        {
            int filesCount = 3;

            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var appcConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();
            var files = this.fixture.Build<RemoteVideoFile>()
                .CreateMany(filesCount)
                .ToArray();

            this.SetupClientSucessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientLogout();
            this.SetupUpdateProgress();
            this.clientMock.SetupSequence(x => x.StartDownload(It.IsAny<RemoteVideoFile>()))
                .Returns(false)
                .Returns(false)
                .Returns(true);

            this.clientMock.Setup(x => x.IsDownloading).Returns(false);

            this.clientMock.Setup(x => x.FindAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(files);

            // act
            var downloader = new HikDownloader(appcConfig, this.container, 100);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Logout(), Times.Once);
            this.clientMock.Verify(x => x.StartDownload(It.IsAny<RemoteVideoFile>()), Times.Exactly(filesCount));
            this.clientMock.Verify(x => x.UpdateProgress(), Times.Once);
            this.clientMock.VerifyGet(x => x.IsDownloading, Times.Once);
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task DownloadAsync_FindOneFiles_LongRunningDownload()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var appcConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();
            var file = this.fixture.Build<RemoteVideoFile>()
                .Create();

            this.SetupClientSucessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientLogout();
            this.SetupUpdateProgress();
            this.clientMock.Setup(x => x.StartDownload(file)).Returns(true).Verifiable();
            this.clientMock.SetupSequence(x => x.IsDownloading)
                .Returns(true)
                .Returns(true)
                .Returns(false);

            this.clientMock.Setup(x => x.FindAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new RemoteVideoFile[] { file });

            // act
            var downloader = new HikDownloader(appcConfig, this.container, 100);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify();
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Logout(), Times.Once);
            this.clientMock.Verify(x => x.StartDownload(file), Times.Once);
            this.clientMock.Verify(x => x.UpdateProgress(), Times.Exactly(3));
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task DownloadAsync_MultipleCameras_OneFilePerCameraToDownload()
        {
            int cameraCount = 3;
            var cameraConfigs = this.fixture.Build<CameraConfig>()
                .CreateMany(cameraCount)
                .ToArray();
            var appcConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, cameraConfigs)
                    .Create();
            var file = this.fixture.Build<RemoteVideoFile>()
                .Create();

            this.SetupClientSucessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientLogout();
            this.SetupUpdateProgress();
            this.clientMock.Setup(x => x.StartDownload(file)).Returns(true).Verifiable();
            this.clientMock.SetupGet(x => x.IsDownloading).Returns(false);

            this.clientMock.Setup(x => x.FindAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new RemoteVideoFile[] { file });

            // act
            var downloader = new HikDownloader(appcConfig, this.container, 100);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify();
            this.clientMock.Verify(x => x.InitializeClient(), Times.Exactly(cameraCount));
            this.clientMock.Verify(x => x.Login(), Times.Exactly(cameraCount));
            this.clientMock.Verify(x => x.Logout(), Times.Exactly(cameraCount));
            this.clientMock.Verify(x => x.StartDownload(file), Times.Exactly(cameraCount));
            this.clientMock.Verify(x => x.UpdateProgress(), Times.Exactly(cameraCount));
            this.directoryMock.Verify(x => x.GetTotalFreeSpace(It.IsAny<string>()), Times.Exactly(cameraCount));
            this.directoryMock.Verify(x => x.DirSize(It.IsAny<string>()), Times.Exactly(cameraCount));
        }

        [Fact]
        public void ForceExit_ClientNotCreated_LogWarning()
        {
            var appcConfig = this.fixture.Build<AppConfig>()
                .Create();

            // act
            var downloader = new HikDownloader(appcConfig, this.container);
            downloader.Cancel();

            // assert
            this.loggerMock.Verify(x => x.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ForceExit_CancelattionOnCleintItitialize_ClientForceExited()
        {
            var appcConfig = this.fixture.Build<AppConfig>()
                    .Create();

            this.clientMock.Setup(x => x.InitializeClient());
            this.clientMock.Setup(x => x.ForceExit());

            // act
            var downloader = new HikDownloader(appcConfig, this.container, 100);

            this.clientMock.Setup(x => x.InitializeClient()).Callback(downloader.Cancel);

            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.ForceExit(), Times.Once);
            this.loggerMock.Verify(x => x.Warn(It.IsAny<string>()), Times.AtLeast(2));
        }

        private void SetupDirectoryHelper()
        {
            this.directoryMock.Setup(x => x.GetTotalFreeSpace(It.IsAny<string>())).Returns(1024);
            this.directoryMock.Setup(x => x.DirSize(It.IsAny<string>())).Returns(1024);
        }

        private void SetupClientSucessLogin()
        {
            this.SetupClientInitialize();
            this.clientMock.Setup(x => x.Login()).Returns(true);
        }

        private void SetupClientInitialize()
        {
            this.clientMock.Setup(x => x.InitializeClient());
        }

        private void SetupClientLogout()
        {
            this.clientMock.Setup(x => x.Logout());
        }

        private void SetupUpdateProgress()
        {
            this.clientMock.Setup(x => x.UpdateProgress());
        }

        private void VerifyStatisticWasPrinted()
        {
            this.directoryMock.Verify(x => x.GetTotalFreeSpace(It.IsAny<string>()), Times.Once);
            this.directoryMock.Verify(x => x.DirSize(It.IsAny<string>()), Times.Once);
        }
    }
}

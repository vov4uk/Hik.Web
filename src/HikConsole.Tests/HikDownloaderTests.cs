using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoFixture;
using HikApi;
using HikApi.Data;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using Moq;
using Xunit;

namespace HikConsole.Tests
{
    public class HikDownloaderTests
    {
        private readonly Mock<ILogger> loggerMock;
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Mock<IEmailHelper> emailMock;
        private readonly Fixture fixture;
        private readonly Mock<IHikClient> clientMock;
        private readonly Mock<IHikClientFactory> clientFactoryMock;
        private readonly Mock<IProgressBarFactory> progressMock;
        private readonly Mock<IUnitOfWorkFactory> uowFactoryMock;
        private readonly Mock<IUnitOfWork> uowMock;
        private readonly Mock<IBaseRepository<Job>> jobRepoMock;
        private readonly Mock<IBaseRepository<HardDriveStatus>> hdRepoMock;
        private readonly Mock<IBaseRepository<Video>> videoRepoMock;
        private readonly Mock<IBaseRepository<Camera>> cameraRepoMock;
        private readonly Mock<IBaseRepository<Photo>> photoRepoMock;

        public HikDownloaderTests()
        {
            this.loggerMock = new Mock<ILogger>();
            this.directoryMock = new Mock<IDirectoryHelper>(MockBehavior.Strict);
            this.emailMock = new Mock<IEmailHelper>(MockBehavior.Strict);
            this.clientMock = new Mock<IHikClient>(MockBehavior.Strict);
            this.clientFactoryMock = new Mock<IHikClientFactory>(MockBehavior.Strict);
            this.progressMock = new Mock<IProgressBarFactory>();
            this.uowFactoryMock = new Mock<IUnitOfWorkFactory>();
            this.uowMock = new Mock<IUnitOfWork>();
            this.jobRepoMock = new Mock<IBaseRepository<Job>>();
            this.hdRepoMock = new Mock<IBaseRepository<HardDriveStatus>>();
            this.videoRepoMock = new Mock<IBaseRepository<Video>>();
            this.cameraRepoMock = new Mock<IBaseRepository<Camera>>();
            this.photoRepoMock = new Mock<IBaseRepository<Photo>>();

            // temp fix, till not cover photo download with UT
            this.clientMock.Setup(x => x.FindPhotosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<RemotePhotoFile>());

            this.SetupCreateJobInstance();
            this.cameraRepoMock.Setup(x => x.FindBy(It.IsAny<Expression<Func<Camera, bool>>>()))
                .ReturnsAsync(new Camera());
            this.uowMock.Setup(x => x.GetRepository<Job>()).Returns(this.jobRepoMock.Object);
            this.uowMock.Setup(x => x.GetRepository<HardDriveStatus>()).Returns(this.hdRepoMock.Object);
            this.uowMock.Setup(x => x.GetRepository<Video>()).Returns(this.videoRepoMock.Object);
            this.uowMock.Setup(x => x.GetRepository<Camera>()).Returns(this.cameraRepoMock.Object);
            this.uowMock.Setup(x => x.GetRepository<Photo>()).Returns(this.photoRepoMock.Object);
            this.uowMock.Setup(x => x.SaveChangesAsync());
            this.uowFactoryMock.Setup(x => x.CreateUnitOfWork(It.IsAny<string>())).Returns(this.uowMock.Object);

            this.fixture = new Fixture();
            var status = this.fixture.Build<HdInfo>().With(x => x.HdStatus, 0U).Create();
            this.clientMock.Setup(x => x.CheckHardDriveStatus()).Returns(status);
        }

        [Fact]
        public async Task DownloadAsync_EmptyConfig_NothingToDo()
        {
            var appConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { })
                    .Create();
            var downloader = this.CreateHikDownloader(appConfig);

            await downloader.DownloadAsync();

            this.clientMock.Verify(x => x.InitializeClient(), Times.Never);
        }

        [Fact]
        public async Task DownloadAsync_LoginFailed_LogWarning()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var appConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();

            this.SetupClientInitialize();
            this.SetupClientDispose();
            this.clientMock.Setup(x => x.Login()).Returns(false);

            // act
            var downloader = this.CreateHikDownloader(appConfig);
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
            var appConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();

            this.SetupClientInitialize();
            this.clientMock.Setup(x => x.ForceExit());
            this.clientMock.Setup(x => x.Login()).Throws(new HikException("Login", 7));
            this.emailMock.Setup(x => x.SendEmail(appConfig.EmailConfig, It.IsAny<string>()));

            // act
            var downloader = this.CreateHikDownloader(appConfig);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.loggerMock.Verify(x => x.Error(It.IsAny<string>(), It.IsAny<Exception>()), Times.Once);
            this.emailMock.Verify(x => x.SendEmail(appConfig.EmailConfig, It.IsAny<string>()), Times.Once);
            this.clientMock.Verify(x => x.ForceExit(), Times.Once);
        }

        [Fact]
        public async Task DownloadAsync_FindNoFiles_NothingToDownload()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var appConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
                    .Create();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();

            this.clientMock.Setup(x => x.FindVideosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new RemoteVideoFile[] { });

            // act
            var downloader = this.CreateHikDownloader(appConfig);
            await downloader.DownloadAsync();

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
            var appConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
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
            var downloader = this.CreateHikDownloader(appConfig);
            await downloader.DownloadAsync();

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
            var appConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
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
            var downloader = this.CreateHikDownloader(appConfig);
            await downloader.DownloadAsync();

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
            var appConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
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
            var downloader = this.CreateHikDownloader(appConfig);
            await downloader.DownloadAsync();

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
            var appConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, new CameraConfig[] { cameraConfig })
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
            var downloader = this.CreateHikDownloader(appConfig);
            await downloader.DownloadAsync();

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
        public async Task DownloadAsync_MultipleCameras_OneFilePerCameraToDownload()
        {
            int cameraCount = 3;
            var cameraConfigs = this.fixture.Build<CameraConfig>()
                .CreateMany(cameraCount)
                .ToArray();
            var appConfig = this.fixture.Build<AppConfig>()
                    .With(x => x.Cameras, cameraConfigs)
                    .Create();
            var file = this.fixture.Build<RemoteVideoFile>()
                .Create();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();
            this.SetupUpdateProgress();
            this.clientMock.Setup(x => x.StartVideoDownload(It.IsAny<RemoteVideoFile>())).Returns(true).Verifiable();
            this.clientMock.SetupGet(x => x.IsDownloading).Returns(false);

            this.clientMock.Setup(x => x.FindVideosAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new RemoteVideoFile[] { file, file });

            // act
            var downloader = this.CreateHikDownloader(appConfig);
            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify();
            this.clientMock.Verify(x => x.InitializeClient(), Times.Exactly(cameraCount));
            this.clientMock.Verify(x => x.Login(), Times.Exactly(cameraCount));
            this.clientMock.Verify(x => x.Dispose(), Times.Exactly(cameraCount));
            this.clientMock.Verify(x => x.StartVideoDownload(It.IsAny<RemoteVideoFile>()), Times.Exactly(cameraCount));
            this.clientMock.Verify(x => x.UpdateVideoProgress(), Times.Exactly(cameraCount));
            this.directoryMock.Verify(x => x.GetTotalFreeSpace(It.IsAny<string>()), Times.Exactly(cameraCount));
            this.directoryMock.Verify(x => x.DirSize(It.IsAny<string>()), Times.Exactly(cameraCount));
        }

        [Fact]
        public void Cancel_ClientNotCreated_LogWarning()
        {
            var appConfig = this.fixture.Build<AppConfig>()
                .Create();

            // act
            var downloader = this.CreateHikDownloader(appConfig);
            downloader.Cancel();

            // assert
            this.loggerMock.Verify(x => x.Warn(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Cancel_CancelationOnClintItitialize_ClientForceExited()
        {
            var appConfig = this.fixture.Build<AppConfig>()
                    .Create();

            this.clientMock.Setup(x => x.InitializeClient());
            this.clientMock.Setup(x => x.ForceExit());
            this.SetupClientDispose();
            this.SetupCreateJobInstance();

            // act
            var downloader = this.CreateHikDownloader(appConfig);

            this.clientMock.Setup(x => x.InitializeClient()).Callback(downloader.Cancel);

            await downloader.DownloadAsync();

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.ForceExit(), Times.Once);
            this.loggerMock.Verify(x => x.Warn(It.IsAny<string>()), Times.Once);
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

        private HikDownloader CreateHikDownloader(AppConfig appConfig)
        {
            this.clientFactoryMock.Setup(x => x.Create(It.IsAny<CameraConfig>())).Returns(this.clientMock.Object);

            return new HikDownloader(appConfig, this.loggerMock.Object, this.emailMock.Object, this.directoryMock.Object, this.clientFactoryMock.Object, this.progressMock.Object, this.uowFactoryMock.Object)
            {
                ProgressCheckPeriodMilliseconds = 100
            };
        }

        private void SetupCreateJobInstance()
        {
            this.jobRepoMock.Setup(x => x.Add(It.IsAny<Job>()));
            this.jobRepoMock.Setup(x => x.Update(It.IsAny<Job>()));
        }
    }
}

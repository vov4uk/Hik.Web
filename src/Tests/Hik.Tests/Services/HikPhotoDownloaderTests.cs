using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using Hik.Client.Abstraction;
using Hik.Client.Service;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Moq;
using Xunit;

namespace Hik.Client.Tests.Services
{
    public class HikPhotoDownloaderTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Fixture fixture;
        private readonly Mock<IClient> clientMock;
        private readonly Mock<IClientFactory> clientFactoryMock;

        public HikPhotoDownloaderTests()
        {
            this.directoryMock = new Mock<IDirectoryHelper>(MockBehavior.Strict);
            this.clientMock = new Mock<IClient>(MockBehavior.Strict);
            this.clientFactoryMock = new Mock<IClientFactory>(MockBehavior.Strict);

            this.fixture = new Fixture();
        }

        [Fact]
        public void ExecuteAsync_EmptyConfig_ExceptionThrown()
        {
            bool success = true;
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>())).Returns(true);

            var downloader = CreateHikDownloader();
            downloader.ExceptionFired += (object sender, Hik.Client.Events.ExceptionEventArgs e) =>
            {
                success = false;
            };

            Assert.ThrowsAsync<ArgumentNullException>(() => downloader.ExecuteAsync(default, default(DateTime), default(DateTime)));

            this.clientMock.Verify(x => x.InitializeClient(), Times.Never);
            Assert.False(success);
        }

        [Fact]
        public async Task ExecuteAsync_FindManyFiles_AllFilesDownloaded()
        {
            int filesCount = 5;

            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var files = this.fixture.Build<MediaFileDTO>()
                .CreateMany(filesCount)
                .ToArray();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();
            this.clientMock.Setup(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            this.clientMock.Setup(x => x.GetFilesListAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(files);

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.clientMock.Verify(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>()), Times.Exactly(filesCount));
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task ExecuteAsync_FindManyFiles_OnlyOneStartDownload()
        {
            int filesCount = 5;

            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var files = this.fixture.Build<MediaFileDTO>()
                .CreateMany(filesCount)
                .ToArray();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();
            this.clientMock.SetupSequence(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false)
                .ReturnsAsync(false)
                .ReturnsAsync(false)
                .ReturnsAsync(true);

            this.clientMock.Setup(x => x.GetFilesListAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(files);

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.clientMock.Verify(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>()), Times.Exactly(filesCount));
        }

        [Fact]
        public async Task ExecuteAsync_CancelationOnClintItitialize_ClientDisposed()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.clientMock.Setup(x => x.InitializeClient());
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>())).Returns(true);
            this.SetupClientDispose();

            // act
            var downloader = this.CreateHikDownloader();

            this.clientMock.Setup(x => x.InitializeClient()).Callback(downloader.Cancel);

            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
        }     
        
        [Fact]
        public async Task ExecuteAsync_CancelationOnDownload_ExceptionFiredAfterFirtstFileDownloaded()
        {
            bool isOperationCanceledException = false;
            int filesCount = 5;

            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();
            var files = this.fixture.Build<MediaFileDTO>()
                .CreateMany(filesCount)
                .ToArray();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();
            this.clientMock.Setup(x => x.GetFilesListAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(files);

            // act
            var downloader = this.CreateHikDownloader();
            this.clientMock.Setup(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>()))
                .Callback(downloader.Cancel)
                .ReturnsAsync(true);
            downloader.ExceptionFired += (object sender, Hik.Client.Events.ExceptionEventArgs e) =>
            {
                isOperationCanceledException = e.Exception is OperationCanceledException;
            };
            
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>()), Times.Once);
            Assert.True(isOperationCanceledException);
        }      
        
        [Fact]
        public async Task ExecuteAsync_CancelationOnLogin_ExceptionFiredGetRemoteFilesListNotStarted()
        {
            bool isOperationCanceledException = false;
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.clientMock.Setup(x => x.InitializeClient());

            this.clientMock.Setup(x => x.Dispose());
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>())).Returns(true);
            this.SetupClientDispose();
            this.clientMock.Setup(x => x.InitializeClient());

            // act
            var downloader = this.CreateHikDownloader();
            this.clientMock.Setup(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>()));
            this.clientMock.Setup(x => x.Login()).Callback(downloader.Cancel).Returns(true);
            downloader.ExceptionFired += (object sender, Hik.Client.Events.ExceptionEventArgs e) =>
            {
                isOperationCanceledException = e.Exception is OperationCanceledException;
            };
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            Assert.True(isOperationCanceledException);
        }

        private void SetupDirectoryHelper()
        {
            this.directoryMock.Setup(x => x.GetTotalFreeSpaceBytes(It.IsAny<string>())).Returns(1024);
            this.directoryMock.Setup(x => x.DirSize(It.IsAny<string>())).Returns(1024);
            this.directoryMock.Setup(x => x.DirExist(It.IsAny<string>())).Returns(true);
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

        private void VerifyStatisticWasPrinted()
        {
            this.directoryMock.Verify(x => x.GetTotalFreeSpaceBytes(It.IsAny<string>()), Times.Once);
            this.directoryMock.Verify(x => x.DirSize(It.IsAny<string>()), Times.Once);
        }

        private HikPhotoDownloaderService CreateHikDownloader()
        {
            this.clientFactoryMock.Setup(x => x.Create(It.IsAny<CameraConfig>())).Returns(this.clientMock.Object);

            return new HikPhotoDownloaderService(this.directoryMock.Object, this.clientFactoryMock.Object);
        }
    }
}

namespace Hik.Client.Services.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Hik.Api;
    using Hik.Api.Data;
    using Hik.Client.Abstraction;
    using Hik.Client.Infrastructure;
    using Hik.Client.Service;
    using Hik.DTO.Config;
    using Hik.DTO.Contracts;
    using Moq;
    using Xunit;

    public class HikVideoDownloaderTests
    {
        private readonly Mock<IDirectoryHelper> directoryMock;
        private readonly Fixture fixture;
        private readonly Mock<IClient> clientMock;
        private readonly Mock<IClientFactory> clientFactoryMock;

        public HikVideoDownloaderTests()
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
            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            var downloader = CreateHikDownloader();
            downloader.ExceptionFired += (object sender, Hik.Client.Events.ExceptionEventArgs e) =>
            {
                success = false;
            };

            Assert.ThrowsAsync<NullReferenceException>(() => downloader.ExecuteAsync(default, default(DateTime), default(DateTime)));

            this.clientMock.Verify(x => x.InitializeClient(), Times.Never);
            Assert.False(success);
        }

        [Fact]
        public async Task ExecuteAsync_LoginFailed_DownloadingNotStarted()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.SetupDirectoryHelper();
            this.SetupClientInitialize();
            this.SetupClientDispose();
            this.clientMock.Setup(x => x.Login()).Returns(false);
            this.directoryMock.Setup(x => x.GetTotalFreeSpace(It.IsAny<string>())).Returns(0);

            // act
            var downloader = this.CreateHikDownloader();
            var result = await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            Assert.Empty(result);
        }  
        
        [Fact]
        public async Task ExecuteAsync_DestinationFolderNotExist_Exit()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.directoryMock.Setup(x => x.DirectoryExists(cameraConfig.DestinationFolder)).Returns(false);

            // act
            var downloader = this.CreateHikDownloader();
            var result = await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            Assert.Null(result);
            this.directoryMock.Verify(x => x.DirectoryExists(cameraConfig.DestinationFolder), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_LoginThrowsException_HandleException()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.SetupClientInitialize();
            this.clientMock.Setup(x => x.ForceExit());
            this.clientMock.Setup(x => x.Login()).Throws(new HikException("Login", 7));
            this.clientMock.Setup(x => x.Dispose());
            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_FindNoFiles_NothingToDownload()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();

            this.clientMock.Setup(x => x.GetFilesListAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(Array.Empty<MediaFileDTO>());

            // act
            var downloader = this.CreateHikDownloader();
            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_FindOneFile_FileDownloaded()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>().Create();
            var file = this.fixture.Build<MediaFileDTO>().Create();
            bool fileDownloaded = false;

            this.SetupClientSuccessLogin();
            this.SetupDirectoryHelper();
            this.SetupClientDispose();
            this.clientMock.Setup(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

            this.clientMock.Setup(x => x.GetFilesListAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>())).ReturnsAsync(new MediaFileDTO[] { file, file });

            // act
            var downloader = this.CreateHikDownloader();
            downloader.FileDownloaded += (object sender, Hik.Client.Events.FileDownloadedEventArgs e) =>
            {
                fileDownloaded = true;
            };

            await downloader.ExecuteAsync(cameraConfig, default(DateTime), default(DateTime));

            // assert
            this.clientMock.Verify(x => x.InitializeClient(), Times.Once);
            this.clientMock.Verify(x => x.Login(), Times.Once);
            this.clientMock.Verify(x => x.Dispose(), Times.Once);
            this.VerifyStatisticWasPrinted();
            Assert.True(fileDownloaded);
        }

        [Fact]
        public async Task ExecuteAsync_FindManyFiles_LastFileNotDownloaded()
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
            this.clientMock.Verify(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>()), Times.Exactly(filesCount - 1));
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
            this.clientMock.Verify(x => x.DownloadFileAsync(It.IsAny<MediaFileDTO>(), It.IsAny<CancellationToken>()), Times.Exactly(filesCount - 1));
            this.VerifyStatisticWasPrinted();
        }

        [Fact]
        public async Task ExecuteAsync_CancelationOnClintItitialize_ClientDisposed()
        {
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.clientMock.Setup(x => x.InitializeClient());
            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
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
        public void Cancel_DownloadNotStarted_NothiningToCancel()
        {
            bool isOperationCanceledException = false;

            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            // act
            var downloader = this.CreateHikDownloader();
            downloader.ExceptionFired += (object sender, Hik.Client.Events.ExceptionEventArgs e) =>
            {
                isOperationCanceledException = e.Exception is OperationCanceledException;
            };
            
            downloader.Cancel();

            // assert
            Assert.False(isOperationCanceledException);
        }      
        
        [Fact]
        public async Task ExecuteAsync_CancelationOnLogin_ExceptionFiredGetRemoteFilesListNotStarted()
        {
            bool isOperationCanceledException = false;
            var cameraConfig = this.fixture.Build<CameraConfig>()
                .Create();

            this.clientMock.Setup(x => x.InitializeClient());

            this.clientMock.Setup(x => x.Dispose());
            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
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
            this.directoryMock.Setup(x => x.GetTotalFreeSpace(It.IsAny<string>())).Returns(1024);
            this.directoryMock.Setup(x => x.DirSize(It.IsAny<string>())).Returns(1024);
            this.directoryMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
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
            this.directoryMock.Verify(x => x.GetTotalFreeSpace(It.IsAny<string>()), Times.Once);
            this.directoryMock.Verify(x => x.DirSize(It.IsAny<string>()), Times.Once);
        }

        private HikVideoDownloaderService CreateHikDownloader()
        {
            this.clientFactoryMock.Setup(x => x.Create(It.IsAny<CameraConfig>())).Returns(this.clientMock.Object);

            return new HikVideoDownloaderService(this.directoryMock.Object, this.clientFactoryMock.Object);
        }
    }
}

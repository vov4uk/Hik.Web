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
    using Hik.Api.Abstraction;
    using Hik.Api.Data;
    using Hik.Client;
    using Hik.Client.Infrastructure;
    using Hik.DTO.Config;
    using Hik.DTO.Contracts;
    using Hik.Helpers.Abstraction;
    using Serilog;
    using Moq;
    using Xunit;
    using DeviceConfig = DTO.Config.DeviceConfig;

    public class HikPhotoClientTests
    {
        private readonly Mock<IHikSDK> sdkMock;
        private readonly Mock<IHikApi> apiMock;
        private readonly Mock<IPhotoService> photoServiceMock;
        private readonly Mock<IConfigService> configServiceMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IDirectoryHelper> dirMock;
        private readonly Mock<IImageHelper> imageMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Fixture fixture;
        private readonly IMapper mapper;

        public HikPhotoClientTests()
        {
            photoServiceMock = new (MockBehavior.Strict);
            configServiceMock = new (MockBehavior.Strict);

            sdkMock = new(MockBehavior.Strict);
            apiMock = new(MockBehavior.Strict);
            apiMock.SetupGet(x => x.PhotoService)
                .Returns(photoServiceMock.Object);
            apiMock.SetupGet(x => x.ConfigService)
                .Returns(configServiceMock.Object);
            loggerMock = new();
            filesMock = new(MockBehavior.Strict);
            dirMock = new(MockBehavior.Strict);
            imageMock = new(MockBehavior.Strict);
            fixture = new();
            Action<IMapperConfigurationExpression> configureAutoMapper = x =>
            {
                x.AddProfile<HikConsoleProfile>();
            };

            var mapperConfig = new MapperConfiguration(configureAutoMapper);
            mapper = mapperConfig.CreateMapper();
        }

        #region GetFilesListAsync
        [Fact]
        public async Task GetFilesListAsync_CallWithValidParameters_ReturnMappedFiles()
        {
            DateTime start = default;
            DateTime end = start.AddSeconds(1);
            var remoteFile = fixture.Create<HikRemoteFile>();

            SetupLoginAndHddStatusCheck();
            photoServiceMock.Setup(x => x.FindFilesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<HikRemoteFile>() { remoteFile });

            var client = GetHikClient();
            client.Login();
            var mediaFiles = await client.GetFilesListAsync(start, end);

            photoServiceMock.Verify(x => x.FindFilesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
            Assert.Single(mediaFiles);
            var firstFile = mediaFiles.First();
            Assert.Equal(remoteFile.Name, firstFile.Name);
            Assert.Equal(remoteFile.Date, firstFile.Date);
            Assert.Equal(remoteFile.Duration, firstFile.Duration);
            Assert.Equal(remoteFile.Size, firstFile.Size);
        }

        #endregion GetFilesListAsync

        #region DownloadFileAsync

        [Theory]
        [InlineData(1991, 05, 31, "19910531_000000.jpg", "C:\\1991-05\\31\\00\\")]
        [InlineData(2020, 12, 31, "20201231_000000.jpg", "C:\\2020-12\\31\\00\\")]
        public async Task DownloadFileAsync_CallDownload_ProperFilesStored(int y, int m, int d, string localFileName, string localFolder)
        {
            var cameraConfig = new CameraConfig { ClientType = ClientType.HikVisionVideo, DestinationFolder = "C:\\", Camera = new DeviceConfig() };

            SetupLoginAndHddStatusCheck();
            MediaFileDto remoteFile = new() { Date = new DateTime(y, m, d) };

            var targetName = localFolder + localFileName;

            filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns((string[] arg) => Path.Combine(arg));
            dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            filesMock.Setup(x => x.FileSize(targetName))
                .Returns(1);
            filesMock.Setup(x => x.RenameFile(localFileName, targetName));
            filesMock.Setup(x => x.FileExists(targetName))
                .Returns(false);
            imageMock.Setup(x => x.SetDate(It.IsAny<string>(), It.IsAny<DateTime>()));

            photoServiceMock.Setup(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>()));

            var client = new HikPhotoClient(cameraConfig, sdkMock.Object, filesMock.Object, dirMock.Object, mapper, this.imageMock.Object, loggerMock.Object);
            client.Login();
            var isDownloaded = await client.DownloadFileAsync(remoteFile, CancellationToken.None);

            Assert.True(isDownloaded);
            filesMock.Verify(x => x.CombinePath(It.IsAny<string[]>()), Times.Exactly(2));
            dirMock.Verify(x => x.CreateDirIfNotExist(It.IsAny<string>()), Times.Once);
            filesMock.Verify(x => x.FileExists(targetName), Times.Once);
            photoServiceMock.Verify(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task DownloadFileAsync_FileAlreadyExist_ReturnFalse()
        {
            filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns(string.Empty);
            dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            filesMock.Setup(x => x.FileExists(It.IsAny<string>()))
                .Returns(true);

            var client = GetHikClient();
            var isDownloaded = await client.DownloadFileAsync(fixture.Create<MediaFileDto>(), CancellationToken.None);

            Assert.False(isDownloaded);
        }

        #endregion DownloadFileAsync

        #region ForceExit
        [Fact]
        public void ForceExit_FilesNotDownloading_DoNotDeleteFile()
        {
            this.SetupLoginAndHddStatusCheck();

            this.apiMock.Setup(x => x.Logout());
            this.sdkMock.Setup(x => x.Cleanup());

            var client = this.GetHikClient();
            client.Login();
            client.ForceExit();

            this.apiMock.Verify(x => x.Logout(), Times.Once);
            this.sdkMock.Verify(x => x.Cleanup(), Times.Once);
        }
        #endregion ForceExit

        private void SetupLoginAndHddStatusCheck()
        {
            var status = new HdInfo { HdStatus = 0 };
            sdkMock.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(apiMock.Object);
            configServiceMock.Setup(x => x.GetHddStatus(It.IsAny<int>()))
                .Returns(status);
        }

        private HikPhotoClient GetHikClient() =>
            new HikPhotoClient(fixture.Create<CameraConfig>(), sdkMock.Object, filesMock.Object, this.dirMock.Object, mapper, imageMock.Object, loggerMock.Object);

    }
}

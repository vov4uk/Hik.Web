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
    using Hik.Api.Services;
    using Hik.Client;
    using Hik.Client.Infrastructure;
    using Hik.DTO.Config;
    using Hik.DTO.Contracts;
    using Hik.Helpers.Abstraction;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;
    using DeviceConfig = DTO.Config.DeviceConfig;

    public class HikPhotoClientTests
    {
        private const int DefaultUserId = 1;
        private readonly Mock<IHikApi> sdkMock;
        private readonly Mock<HikPhotoService> photoServiceMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IDirectoryHelper> dirMock;
        private readonly Mock<IImageHelper> imageMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Fixture fixture;
        private readonly IMapper mapper;

        public HikPhotoClientTests()
        {
            photoServiceMock = new (MockBehavior.Strict);

            sdkMock = new(MockBehavior.Strict);
            sdkMock.SetupGet(x => x.PhotoService)
                .Returns(photoServiceMock.Object);
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
        public async Task GetFilesListAsync_CallWithValidParameters_ReturnMapppedFiles()
        {
            DateTime start = default;
            DateTime end = start.AddSeconds(1);
            var remoteFile = fixture.Create<HikRemoteFile>();

            photoServiceMock.Setup(x => x.FindFilesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Session>()))
                .ReturnsAsync(new List<HikRemoteFile>() { remoteFile });

            var client = GetHikClient();
            var mediaFiles = await client.GetFilesListAsync(start, end);

            photoServiceMock.Verify(x => x.FindFilesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<Session>()), Times.Once);
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
            var cameraConfig = new CameraConfig { ClientType = ClientType.HikVisionVideo, DestinationFolder = "C:\\", Alias = "test", Camera = new DeviceConfig() };

            SetupLoginAndHddStatusCheck();
            MediaFileDto remoteFile = new () { Date = new DateTime(y, m, d)};

            var targetName = localFolder + localFileName;

            filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>()))
                .Returns((string[] arg) => Path.Combine(arg));
            dirMock.Setup(x => x.CreateDirIfNotExist(It.IsAny<string>()));
            filesMock.Setup(x => x.FileSize(targetName))
                .Returns(1);
            filesMock.Setup(x => x.DeleteFile(localFileName));
            filesMock.Setup(x => x.FileExists(targetName))
                .Returns(false);
            imageMock.Setup(x => x.SetDate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()));

            photoServiceMock.Setup(x => x.DownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>()));

            var client = new HikPhotoClient(cameraConfig, sdkMock.Object, filesMock.Object, dirMock.Object, mapper, this.imageMock.Object, loggerMock.Object);
            client.Login();
            var isDownloaded = await client.DownloadFileAsync(remoteFile, CancellationToken.None);

            Assert.True(isDownloaded);
            filesMock.Verify(x => x.CombinePath(It.IsAny<string[]>()), Times.Exactly(2));
            dirMock.Verify(x => x.CreateDirIfNotExist(It.IsAny<string>()), Times.Once);
            filesMock.Verify(x => x.FileExists(targetName), Times.Once);
            photoServiceMock.Verify(x => x.DownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>()), Times.Once);
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

            this.sdkMock.Setup(x => x.Logout(DefaultUserId));
            this.sdkMock.Setup(x => x.Cleanup());

            var client = this.GetHikClient();
            client.Login();
            client.ForceExit();

            this.sdkMock.Verify(x => x.Logout(DefaultUserId), Times.Once);
            this.sdkMock.Verify(x => x.Cleanup(), Times.Once);
        }
        #endregion ForceExit

        private Session SetupLoginAndHddStatusCheck()
        {
            DeviceInfo outDevice = fixture.Create<DeviceInfo>();
            var result = new Session(DefaultUserId, outDevice.DefaultIpChannel, new List<IpChannel>());
            var status = new HdInfo { HdStatus = 0 };
            sdkMock.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(result);
            sdkMock.Setup(x => x.GetHddStatus(DefaultUserId))
                .Returns(status);
            return result;
        }

        private HikPhotoClient GetHikClient() =>
            new HikPhotoClient(fixture.Create<CameraConfig>(), sdkMock.Object, filesMock.Object, this.dirMock.Object, mapper, imageMock.Object, loggerMock.Object);

    }
}

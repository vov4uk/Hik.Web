﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using HikApi.Abstraction;
using HikApi.Data;
using HikConsole;
using HikConsole.Abstraction;
using Moq;
using Xunit;

namespace HikConsoleTests
{
    public class HikClientTests
    {
        private const int DefaultUserId = 1;
        private readonly Mock<IHikApi> sdkMock;
        private readonly Mock<IFilesHelper> filesMock;
        private readonly Mock<IProgressBarFactory> progressMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Fixture fixture;

        public HikClientTests()
        {
            this.sdkMock = new Mock<IHikApi>(MockBehavior.Strict);
            this.filesMock = new Mock<IFilesHelper>(MockBehavior.Strict);
            this.progressMock = new Mock<IProgressBarFactory>(MockBehavior.Strict);
            this.loggerMock = new Mock<ILogger>();
            this.fixture = new Fixture();
        }

        [Fact]
        public void Init_CallInit_ClientInitialized()
        {
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>())).Returns(string.Empty);
            this.filesMock.Setup(x => x.FolderCreateIfNotExist(It.IsAny<string>()));
            this.sdkMock.Setup(x => x.Initialize()).Returns(true);
            this.sdkMock.Setup(x => x.SetupLogs(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);

            var client = this.GetHikClient();
            client.InitializeClient();

            this.sdkMock.Verify(x => x.Initialize(), Times.Once);
            this.sdkMock.Verify(x => x.SetupLogs(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public void Login_CallLogin_LoginSucessfully()
        {
            this.SetupLogin();
            var client = this.GetHikClient();
            bool res = client.Login();

            this.sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            Assert.True(res);
        }

        [Fact]
        public void Login_CallLoginTwice_LoginOnce()
        {
            this.SetupLogin();

            var client = this.GetHikClient();
            var first = client.Login();
            var second = client.Login();

            Assert.True(first);
            Assert.False(second);
            this.sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Logout_CallLogin_LogoutSuccess()
        {
            this.SetupLogin();

            this.sdkMock.Setup(x => x.Logout(DefaultUserId));
            this.sdkMock.Setup(x => x.Cleanup());

            bool loginResult = false;
            using (var client = this.GetHikClient())
            {
                loginResult = client.Login();
            }

            Assert.True(loginResult);
            this.sdkMock.Verify(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.sdkMock.Verify(x => x.Logout(DefaultUserId), Times.Once);
            this.sdkMock.Verify(x => x.Cleanup(), Times.Once);
        }

        [Fact]
        public void Logout_DontCallLogin_LogoutNotCall()
        {
            this.sdkMock.Setup(x => x.Cleanup()).Verifiable();
            using (var client = this.GetHikClient())
            {
                // Do nothing
            }

            this.sdkMock.Verify();
            this.sdkMock.Verify(x => x.Logout(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task FindAsync_CallLogin_CallFindWithCorrectParameters()
        {
            DateTime start = default(DateTime);
            DateTime end = start.AddSeconds(1);
            var result = this.SetupLogin();

            this.sdkMock.Setup(x => x.FindVideoFilesAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(new List<RemoteVideoFile>());

            var client = this.GetHikClient();
            var loginResult = client.Login();

            await client.FindAsync(start, end);

            Assert.True(loginResult);
            this.sdkMock.Verify(x => x.FindVideoFilesAsync(start, end, DefaultUserId, result.Device.StartChannel), Times.Once);
        }

        [Fact]
        public void Find_CallFindWithInvalidParameters_ThrowsException()
        {
            var client = this.GetHikClient();

            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                var date = new DateTime(1970, 1, 1);
                await client.FindAsync(date, date);
            });
        }

        [Fact]
        public void StartDwonload_CallStartDownload_ReturnTrue()
        {
            int downloadHandler = 1;
            this.SetupLogin();
            this.SetupFilesMockForDwonload();

            this.sdkMock.Setup(x => x.StartDownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(downloadHandler);
            this.progressMock.Setup(x => x.Create()).Returns(default(IProgressBar));

            var client = this.GetHikClient();
            client.Login();
            var isDownloading = client.StartDownload(this.fixture.Create<RemoteVideoFile>());

            Assert.True(isDownloading);
            this.filesMock.Verify(x => x.CombinePath(It.IsAny<string[]>()), Times.Exactly(2));
            this.filesMock.Verify(x => x.FolderCreateIfNotExist(It.IsAny<string>()), Times.Once);
            this.filesMock.Verify(x => x.FileExists(It.IsAny<string>(), It.IsAny<long>()), Times.Once);
            this.sdkMock.Verify(x => x.StartDownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.progressMock.Verify(x => x.Create(), Times.Once);
        }

        [Fact]
        public void StartDwonload_FileAlreadyExist_ReturnFalse()
        {
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>())).Returns(string.Empty);
            this.filesMock.Setup(x => x.FolderCreateIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>(), It.IsAny<long>())).Returns(true);

            var client = this.GetHikClient();

            var isDownloading = client.StartDownload(this.fixture.Create<RemoteVideoFile>());

            Assert.False(isDownloading);
        }

        [Fact]
        public void StartDwonload_CallStartDownloadTwice_AlreadyDownloading()
        {
            int downloadHandler = 1;

            this.SetupFilesMockForDwonload();
            this.SetupLogin();
            this.sdkMock.Setup(x => x.StartDownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(downloadHandler);
            this.progressMock.Setup(x => x.Create()).Returns(default(IProgressBar));

            var client = this.GetHikClient();
            client.Login();
            var downloading = client.StartDownload(this.fixture.Create<RemoteVideoFile>());
            var notDownloading = client.StartDownload(this.fixture.Create<RemoteVideoFile>());

            Assert.True(downloading);
            Assert.False(notDownloading);
            this.filesMock.Verify(x => x.CombinePath(It.IsAny<string[]>()), Times.Exactly(2));
            this.filesMock.Verify(x => x.FolderCreateIfNotExist(It.IsAny<string>()), Times.Once);
            this.filesMock.Verify(x => x.FileExists(It.IsAny<string>(), It.IsAny<long>()), Times.Once);
            this.sdkMock.Verify(x => x.StartDownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            this.progressMock.Verify(x => x.Create(), Times.Once);
        }

        [Fact]
        public void StopDownload_FileNotDownloading_DoNothing()
        {
            var client = this.GetHikClient();

            client.StopDownload();

            this.sdkMock.Verify(x => x.StopDownloadFile(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void StopDownload_FileIsDownloading_DoStop()
        {
            var progressBarMock = new Mock<IProgressBar>(MockBehavior.Strict);
            int downloadHandler = 1;
            this.SetupLogin();
            this.SetupFilesMockForDwonload();

            this.sdkMock.Setup(x => x.StartDownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(downloadHandler);
            progressBarMock.Setup(x => x.Dispose());
            this.progressMock.Setup(x => x.Create()).Returns(progressBarMock.Object);
            this.sdkMock.Setup(x => x.StopDownloadFile(downloadHandler));

            var client = this.GetHikClient();
            client.Login();
            var isDownloadinStarted = client.StartDownload(this.fixture.Create<RemoteVideoFile>());
            client.StopDownload();

            Assert.True(isDownloadinStarted);
            this.sdkMock.Verify(x => x.StopDownloadFile(downloadHandler), Times.Once);
            Assert.False(client.IsDownloading);
            progressBarMock.Verify(x => x.Dispose(), Times.Once);
        }

        [Fact]
        public void ForceExit_FilesIsDownloading_DeleteFile()
        {
            int downloadHandler = 1;
            this.SetupLogin();

            this.SetupFilesMockForDwonload();

            this.filesMock.Setup(x => x.DeleteFile(It.IsAny<string>()));
            this.sdkMock.Setup(x => x.StartDownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(downloadHandler);
            this.progressMock.Setup(x => x.Create()).Returns(default(IProgressBar));
            this.sdkMock.Setup(x => x.StopDownloadFile(downloadHandler));
            this.sdkMock.Setup(x => x.Logout(DefaultUserId));
            this.sdkMock.Setup(x => x.Cleanup());

            bool downloading = false;
            HikClient client = null;
            using (client = this.GetHikClient())
            {
                client.Login();
                downloading = client.StartDownload(this.fixture.Create<RemoteVideoFile>());
                client.ForceExit();
            }

            Assert.True(downloading);
            this.sdkMock.Verify(x => x.StopDownloadFile(downloadHandler), Times.Once);
            this.sdkMock.Verify(x => x.Logout(DefaultUserId), Times.Once);
            this.sdkMock.Verify(x => x.Cleanup(), Times.Once);
            this.filesMock.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Once);
            Assert.False(client.IsDownloading);
        }

        [Fact]
        public void ForceExit_FilesNotDownloading_DoNotDeleteFile()
        {
            this.SetupLogin();

            this.sdkMock.Setup(x => x.Logout(DefaultUserId)).Verifiable();
            this.sdkMock.Setup(x => x.Cleanup()).Verifiable();

            using (var client = this.GetHikClient())
            {
                client.Login();

                client.ForceExit();
            }

            this.sdkMock.VerifyAll();
        }

        [Fact]
        public void CheckProgress_FileNotDownloading_DoNothing()
        {
            var client = this.GetHikClient();

            client.UpdateProgress();

            this.sdkMock.Verify(x => x.GetDownloadPosition(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public void CheckProgress_FileIsDownloading_ReportProgress()
        {
            var progressBarMock = new Mock<IProgressBar>(MockBehavior.Strict);
            int downloadHandler = 1;

            this.SetupFilesMockForDwonload();
            this.SetupLogin();
            this.sdkMock.Setup(x => x.StartDownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(downloadHandler);
            this.sdkMock.Setup(x => x.GetDownloadPosition(downloadHandler)).Returns(50);
            progressBarMock.Setup(x => x.Report(0.5));
            this.progressMock.Setup(x => x.Create()).Returns(progressBarMock.Object);

            var client = this.GetHikClient();
            client.Login();
            var downloading = client.StartDownload(this.fixture.Create<RemoteVideoFile>());
            client.UpdateProgress();

            Assert.True(downloading);
            this.sdkMock.Verify(x => x.GetDownloadPosition(It.IsAny<int>()), Times.Once);
            progressBarMock.Verify(x => x.Report(0.5), Times.Once);
        }

        [Fact]
        public void CheckProgress_FileDownloaded_StopDwonload()
        {
            var progressBarMock = new Mock<IProgressBar>(MockBehavior.Strict);
            int downloadHandler = 1;

            this.SetupFilesMockForDwonload();
            this.SetupLogin();
            this.sdkMock.Setup(x => x.StartDownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(downloadHandler);
            this.sdkMock.Setup(x => x.GetDownloadPosition(downloadHandler)).Returns(100);
            this.sdkMock.Setup(x => x.StopDownloadFile(downloadHandler));
            progressBarMock.Setup(x => x.Dispose());
            this.progressMock.Setup(x => x.Create()).Returns(progressBarMock.Object);

            var client = this.GetHikClient();
            client.Login();
            var downloading = client.StartDownload(this.fixture.Create<RemoteVideoFile>());
            client.UpdateProgress();

            Assert.True(downloading);
            this.sdkMock.Verify(x => x.GetDownloadPosition(It.IsAny<int>()), Times.Once);
            progressBarMock.Verify(x => x.Dispose(), Times.Once);
            this.sdkMock.Verify(x => x.StopDownloadFile(downloadHandler), Times.Once);
            Assert.False(client.IsDownloading);
        }

        [Fact]
        public void CheckProgress_ErrorHappen_ThrowsException()
        {
            int downloadHandler = 1;

            this.SetupLogin();
            this.SetupFilesMockForDwonload();

            this.sdkMock.Setup(x => x.StartDownloadFile(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(downloadHandler);
            this.sdkMock.Setup(x => x.GetDownloadPosition(downloadHandler)).Returns(200);
            this.filesMock.Setup(x => x.DeleteFile(It.IsAny<string>()));
            this.progressMock.Setup(x => x.Create()).Returns(default(IProgressBar));
            this.sdkMock.Setup(x => x.StopDownloadFile(downloadHandler));
            this.sdkMock.Setup(x => x.Logout(DefaultUserId));
            this.sdkMock.Setup(x => x.Cleanup());

            var client = this.GetHikClient();
            client.Login();
            var downloading = client.StartDownload(this.fixture.Create<RemoteVideoFile>());

            Assert.Throws<InvalidOperationException>(client.UpdateProgress);

            Assert.True(downloading);
            this.sdkMock.Verify(x => x.GetDownloadPosition(It.IsAny<int>()), Times.Once);
        }

        private Session SetupLogin()
        {
            DeviceInfo outDevice = this.fixture.Create<DeviceInfo>();
            var result = new Session(DefaultUserId, outDevice.ChannelNumber, outDevice.StartChannel);
            this.sdkMock.Setup(x => x.Login(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(result);
            return result;
        }

        private void SetupFilesMockForDwonload()
        {
            this.filesMock.Setup(x => x.CombinePath(It.IsAny<string[]>())).Returns(string.Empty);
            this.filesMock.Setup(x => x.FolderCreateIfNotExist(It.IsAny<string>()));
            this.filesMock.Setup(x => x.FileExists(It.IsAny<string>(), It.IsAny<long>())).Returns(false);
        }

        private HikClient GetHikClient()
        {
            return new HikClient(this.fixture.Create<HikConsole.Config.CameraConfig>(), this.sdkMock.Object, this.filesMock.Object, this.progressMock.Object, this.loggerMock.Object);
        }
    }
}

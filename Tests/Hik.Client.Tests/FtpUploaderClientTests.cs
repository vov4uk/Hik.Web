using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using FluentFTP;
using FluentFTP.Exceptions;
using Hik.Client.Client;
using Hik.DTO.Config;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Hik.Client.Tests
{
    public class FtpUploaderClientTests
    {
        private readonly Mock<IAsyncFtpClient> ftpMock;
        private readonly Mock<ILogger> loggerMock;
        private readonly Fixture fixture;

        public FtpUploaderClientTests()
        {
            this.ftpMock = new(MockBehavior.Strict);
            this.loggerMock = new();
            this.fixture = new();
        }

        [Fact]
        public void Constructor_PutEmptyConfig_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new FtpUploaderClient(null, ftpMock.Object, loggerMock.Object));
        }

        [Fact]
        public void InitializeClient_CallInitializeClient_ClientInitialized()
        {
            var config = new DeviceConfig { IpAddress = "192.168.0.1", UserName = "admin", Password = "admin" };

            var ftp = new AsyncFtpClient();
            var client = new FtpUploaderClient(config, ftp, loggerMock.Object);
            client.InitializeClient();

            Assert.Equal(config.IpAddress, ftp.Host);
            Assert.Equal(config.UserName, ftp.Credentials.UserName);
            Assert.Equal(config.Password, ftp.Credentials.Password);
        }

        [Theory]
        [AutoData]
        public async Task UploadFilesAsync_FailedTwoTimes_Uploaded(IEnumerable<string> files, string remotePath)
        {
            var config = new DeviceConfig { IpAddress = "192.168.0.1", UserName = "admin", Password = "admin" };

            ftpMock.SetupSequence(x => x.UploadFiles(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), FtpRemoteExists.Overwrite, true, FtpVerify.None, FtpError.None, default, null, null))
                .Throws<TimeoutException>()
                .Throws<FtpHashUnsupportedException>()
                .ReturnsAsync(new List<FtpResult>());

            var client = new FtpUploaderClient(config, ftpMock.Object, loggerMock.Object);
            await client.UploadFilesAsync(files, remotePath);

            ftpMock.Verify(x => x.UploadFiles(It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), FtpRemoteExists.Overwrite, true, FtpVerify.None, FtpError.None, default, null, null),
                Times.Exactly(3));
        }
    }
}

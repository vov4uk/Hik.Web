using Hik.Client.Abstraction.Services;
using Hik.Client.Events;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Impl;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Job.Tests.Impl
{
    public class VideoDownloaderJobTests : ServiceJobBaseTests<IHikVideoDownloaderService>
    {
        public VideoDownloaderJobTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task ExecuteAsync_FileDownloaded_SaveFilesAsync()
        {
            dbMock.Setup(x => x.UpdateJob(It.IsAny<HikJob>()));
            SetupCreateJob();
            SetupUpdateJobTrigger();
            dbMock.Setup(x => x.SaveFile(It.IsAny<HikJob>(), It.IsAny<MediaFileDto>()));

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback(() =>
                {
                    this.serviceMock.Raise(mock => mock.FileDownloaded += null,
                        new FileDownloadedEventArgs(new MediaFileDto() { Duration = 1, Name = "video0.mp4" }));
                    this.serviceMock.Raise(mock => mock.FileDownloaded += null,
                        new FileDownloadedEventArgs(new MediaFileDto() { Duration = 1, Name = "video1.mp4" }));
                })
                .ReturnsAsync(new List<MediaFileDto>());

            var job = CreateJob();

            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
            Assert.Equal(2, job.JobInstance.FilesCount);
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionFired_ExceptionHandled()
        {
            SetupCreateJob();
            SetupUpdateJobTrigger();
            SetupLogExceptionToAsync();
            emailMock.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();

            var job = CreateJob();

            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
            Assert.Equal(0, job.JobInstance.FilesCount);
        }

        [Fact]
        public void Constructor_ValidConfig_ValidConfigType()
        {
            var job = CreateJob();
            Assert.IsType<CameraConfig>(job.Config);
        }

        private VideoDownloaderJob CreateJob(string configFileName = "HikVideoTests.json")
        {
            var config = GetConfig(configFileName);
            return new VideoDownloaderJob(new JobTrigger { Group = group, TriggerKey = triggerKey, Config = config, SentEmailOnError = true }, serviceMock.Object, dbMock.Object, this.emailMock.Object, this.loggerMock );
        }

    }
}

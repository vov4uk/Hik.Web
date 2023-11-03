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
    public class HikPhotoDownloaderJobTests : ServiceJobBaseTests<IHikPhotoDownloaderService>
    {
        public HikPhotoDownloaderJobTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task ExecuteAsync_FileDownloaded_SaveFilesAsync()
        {
            var lastSync = new DateTime(2021, 5, 31, 0, 0, 0);

            dbMock.Setup(x => x.GetJobTriggerAsync(group, triggerKey))
                .ReturnsAsync(new JobTrigger() { LastSync = lastSync });
            SetupCreateJobInstance();
            SetupSaveJobResultAsync();
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<MediaFileDto>>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDto>>()))
                .ReturnsAsync(new List<MediaFile>());
            dbMock.Setup(x => x.SaveDownloadHistoryFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFile>>()))
                .Returns(Task.CompletedTask);

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), lastSync.AddMinutes(-1), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<MediaFileDto>() { new MediaFileDto() { Duration = 0, Name = "photo0.jpg" } });

            var job = CreateJob();

            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
            Assert.Equal(1, job.JobInstance.FilesCount);
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionFired_ExceptionHandled()
        {
            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstance();
            SetupSaveJobResultAsync();
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

        private HikPhotoDownloaderJob CreateJob(string configFileName = "HikVideoTests.json")
        {
            var config = GetConfig(configFileName);
            return new HikPhotoDownloaderJob(new JobTrigger { Group = group, TriggerKey = triggerKey, Config = config } , serviceMock.Object, dbMock.Object, this.emailMock.Object, this.loggerMock);
        }
    }
}

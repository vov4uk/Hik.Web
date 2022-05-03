using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Impl;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Job.Tests.Impl
{
    public class HikPhotoDownloaderJobTests : JobBaseTests<IHikPhotoDownloaderService>
    {
        public HikPhotoDownloaderJobTests() : base() { }

        [Fact]
        public async Task ExecuteAsync_FileDownloaded_SaveFilesAsync()
        {
            var lastSync = new DateTime(2021, 5, 31, 0, 0, 0);

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger() { LastSync = lastSync });
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>()))
                .ReturnsAsync(new List<MediaFile>());
            dbMock.Setup(x => x.SaveDownloadHistoryFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFile>>()))
                .Returns(Task.CompletedTask);

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), lastSync.AddMinutes(-1), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<MediaFileDTO>() { new MediaFileDTO() { Duration = 0, Name = "photo0.jpg" } });

            var job = CreateJob();

            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
            Assert.Equal(1, job.JobInstance.FilesCount);
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionFired_ExceptionHandled()
        {
            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);
            emailMock.Setup(x => x.Send(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Callback(() =>
                {
                    this.serviceMock.Raise(mock => mock.ExceptionFired += null,
                        new ExceptionEventArgs(new Exception("Something went wrong")));
                })
                .ReturnsAsync(new List<MediaFileDTO>());

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
            => new HikPhotoDownloaderJob($"{group}.{triggerKey}", Path.Combine(TestsHelper.CurrentDirectory, configFileName), dbMock.Object, this.emailMock.Object, Guid.Empty);
    }
}

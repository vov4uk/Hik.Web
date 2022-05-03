namespace Job.Tests.Impl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Hik.Client.Abstraction;
    using Hik.DataAccess.Data;
    using Hik.DTO.Config;
    using Hik.DTO.Contracts;
    using Job.Impl;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    public class ArchiveJobTests : JobBaseTests<IArchiveService>
    {
        public ArchiveJobTests() : base() { }

    [Fact]
        public async Task ExecuteAsync_NoFilesFound_NothingSavedToDb()
        {
            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(new List<MediaFileDTO>());

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_2FilesFound_StatisticsUpdated()
        {
            var files = new List<MediaFileDTO>()
            {
                new (){Date = new (2022,01,01), Name = "File1", Duration = 0},
                new (){Date = new (2022,01,31), Name = "File2", Duration = 0},
            };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), files))
                .Returns(Task.CompletedTask);
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(files);

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            Assert.Equal(2, job.JobInstance.FilesCount);
            Assert.Equal(new DateTime(2022, 01, 01), job.JobInstance.PeriodStart);
            Assert.Equal(new DateTime(2022, 01, 31), job.JobInstance.PeriodEnd);
        }

        [Fact]
        public async Task ExecuteAsync_AbnormalActivity_EmailSend()
        {
            List<MediaFileDTO> files = new ()
            {
                new (),
                new (),
            };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), files))
                .Returns(Task.CompletedTask);
            emailMock.Setup(x => x.Send($"2 - {group}.{triggerKey}: Abnormal activity detected", It.IsAny<string>()))
                .Verifiable();
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(files);

            var job = CreateJob("ArchiveJobTestsAbnormalActivity.json");
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_VideoFiles_SaveFilesAsync()
        {
            List<MediaFileDTO> files = new ()
            {
                new (){ Duration = 1 },
                new (){ Duration = 1 },
                new (){ Duration = 1 },
            };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>()))
                .ReturnsAsync(new List<MediaFile>());
            dbMock.Setup(x => x.SaveDownloadHistoryFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFile>>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), files))
                .Returns(Task.CompletedTask);

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(files);

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionFired_ExceptionLogged()
        {
            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ThrowsAsync(new Exception("Shit happens"));

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            Assert.False(job.JobInstance.Success);
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionFired_EmailWasSent()
        {
            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ThrowsAsync(new Exception("Shit happens"));
            emailMock.Setup(x => x.Send(It.IsAny<Exception>(),It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();

            var job = CreateJob("ArchiveJobTestsSendEmail.json");
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_FailedToLogException_Handled()
        {
            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Throws<Exception>();

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ThrowsAsync(new Exception("Shit happens"));
            emailMock.Setup(x => x.Send(It.IsAny<Exception>(),It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();

            var job = CreateJob("ArchiveJobTestsSendEmail.json");
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
        }

        [Fact]
        public void Constructor_ConfigNotExist_Exception()
        {
            Assert.Throws<FileNotFoundException>(() => CreateJob(".json"));
        }

        [Fact]
        public void Constructor_InvalidConfig_Exception()
        {
            Assert.Throws<JsonReaderException>(() => CreateJob("ArchiveJobTestsInvalid.json"));
        }

        [Fact]
        public void Constructor_ValidConfig_ValidConfigType()
        {
            var job = CreateJob();
            Assert.IsType<ArchiveConfig>(job.Config);
        }

        private ArchiveJob CreateJob(string configFileName = "ArchiveJobTests.json")
            => new ArchiveJob($"{group}.{triggerKey}", Path.Combine(TestsHelper.CurrentDirectory, configFileName), dbMock.Object, this.emailMock.Object, Guid.Empty);
    }
}

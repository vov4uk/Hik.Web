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

    public class ArchiveJobTests : ServiceJobBaseTests<IArchiveService>
    {
        public ArchiveJobTests() : base() { }

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

        [Fact]
        public async Task ExecuteAsync_2FilesFound_StatisticsUpdated()
        {
            var files = new List<MediaFileDTO>()
            {
                new (){Date = new (2022,01,01), Name = "File1", Duration = 0},
                new (){Date = new (2022,01,31), Name = "File2", Duration = 0},
            };

            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstanceAsync();
            SetupSaveJobResultAsync();
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
            List<MediaFileDTO> files = new()
            {
                new(),
                new(),
            };

            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstanceAsync();
            SetupSaveJobResultAsync();
            SetupUpdateDailyStatisticsAsync(files);
            emailMock.Setup(x => x.Send($"2 - {group}.{triggerKey}: Abnormal activity detected", It.IsAny<string>()))
                .Verifiable();
            SetupExecuteAsync(files);

            var job = CreateJob("ArchiveJobTestsAbnormalActivity.json");
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionFired_EmailWasSent()
        {
            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstanceAsync();
            SetupSaveJobResultAsync();
            SetupLogExceptionToAsync();

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ThrowsAsync(new Exception("Shit happens"));
            emailMock.Setup(x => x.Send(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();

            var job = CreateJob("ArchiveJobTestsSendEmail.json");
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_ExceptionFired_ExceptionLogged()
        {
            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstanceAsync();
            SetupSaveJobResultAsync();
            SetupLogExceptionToAsync();

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ThrowsAsync(new Exception("Shit happens"));

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            Assert.False(job.JobInstance.Success);
        }

        [Fact]
        public async Task ExecuteAsync_FailedToLogException_Handled()
        {
            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstanceAsync();
            SetupSaveJobResultAsync();
            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Throws<Exception>();

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ThrowsAsync(new Exception("Shit happens"));
            emailMock.Setup(x => x.Send(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();

            var job = CreateJob("ArchiveJobTestsSendEmail.json");
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_NoFilesFound_NothingSavedToDb()
        {
            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstanceAsync();
            SetupSaveJobResultAsync();
            SetupExecuteAsync();

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
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

            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstanceAsync();
            SetupSaveJobResultAsync();
            dbMock.Setup(x => x.SaveFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>()))
                .ReturnsAsync(new List<MediaFile>());
            dbMock.Setup(x => x.SaveDownloadHistoryFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFile>>()))
                .Returns(Task.CompletedTask);
            SetupUpdateDailyStatisticsAsync(files);

            SetupExecuteAsync(files);

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
        }
        private ArchiveJob CreateJob(string configFileName = "ArchiveJobTests.json")
        {
            var config = GetConfig<ArchiveConfig>(configFileName);
            return new ArchiveJob($"{group}.{triggerKey}", config, serviceMock.Object, dbMock.Object, this.emailMock.Object, this.loggerMock.Object);
        }
    }
}

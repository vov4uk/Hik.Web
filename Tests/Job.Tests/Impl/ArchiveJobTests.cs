namespace Job.Tests.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Hik.Client.Abstraction.Services;
    using Hik.DataAccess.Data;
    using Hik.DTO.Config;
    using Hik.DTO.Contracts;
    using Job.Impl;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;
    using Xunit.Abstractions;

    public class ArchiveJobTests : ServiceJobBaseTests<IArchiveService>
    {
        public ArchiveJobTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public void Constructor_ConfigNotExist_Exception()
        {
            Assert.Throws<NullReferenceException>(() => 
            new ArchiveJob(new JobTrigger { Group = group, TriggerKey = triggerKey, Config = null }, serviceMock.Object, dbMock.Object, this.emailMock.Object, this.loggerMock));
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
            var files = new List<MediaFileDto>()
            {
                new (){Date = new (2022,01,01), Name = "File1", Duration = 0},
                new (){Date = new (2022,01,31), Name = "File2", Duration = 0},
            };

            SetupCreateJob();
            SetupUpdateJobTrigger();
            SetupUpdateJob();
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<int>(), files))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveFiles(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDto>>()))
                .Returns(new List<MediaFile>());
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
            List<MediaFileDto> files = new()
            {
                new(),
                new(),
            };

            SetupCreateJob();
            SetupUpdateJob();
            SetupUpdateJobTrigger();
            SetupUpdateDailyStatisticsAsync(files);
            dbMock.Setup(x => x.SaveFiles(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDto>>()))
                .Returns(new List<MediaFile>());
            emailMock.Setup(x => x.Send("Key: 2 taken. From 0001-01-01 00:00:00 to 00:00:00", "EOM"))
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
            SetupCreateJob();
            SetupUpdateJob();
            SetupUpdateJobTrigger();
            SetupLogExceptionToAsync();

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ThrowsAsync(new Exception("Shit happens"));
            emailMock.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();

            var job = CreateJob("ArchiveJobTestsSendEmail.json");
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
            Assert.False(job.JobInstance.Success);
        }

        [Fact]
        public async Task ExecuteAsync_FailedToLogException_Handled()
        {
            SetupCreateJob();
            SetupUpdateJobTrigger();

            dbMock.Setup(x => x.LogExceptionTo(It.IsAny<int>(), It.IsAny<string>()))
                .Throws<Exception>();

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ThrowsAsync(new Exception("Shit happens"));
            emailMock.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Verifiable();

            var job = CreateJob("ArchiveJobTestsSendEmail.json");
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_NoFilesFound_NothingSavedToDb()
        {
            SetupCreateJob();
            SetupUpdateJob();
            SetupUpdateJobTrigger();
            SetupExecuteAsync();

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_VideoFiles_SaveFilesAsync()
        {
            List<MediaFileDto> files = new ()
            {
                new (){ Duration = 1 },
                new (){ Duration = 1 },
                new (){ Duration = 1 },
            };

            SetupCreateJob();
            SetupUpdateJob();
            SetupUpdateJobTrigger();
            dbMock.Setup(x => x.SaveFiles(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDto>>()))
                .Returns(new List<MediaFile>());
            SetupUpdateDailyStatisticsAsync(files);

            SetupExecuteAsync(files);

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
        }

        private ArchiveJob CreateJob(string configFileName = "ArchiveJobTests.json")
        {
            var config = GetConfig(configFileName);
            return new ArchiveJob(new JobTrigger { Group = group, TriggerKey = triggerKey, Config = config, SentEmailOnError = true }, serviceMock.Object, dbMock.Object, this.emailMock.Object, this.loggerMock);
        }
    }
}

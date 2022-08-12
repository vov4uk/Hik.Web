namespace Job.Tests.Impl
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Hik.Client.Abstraction;
    using Hik.DataAccess.Data;
    using Hik.DTO.Config;
    using Hik.DTO.Contracts;
    using Job.Impl;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    public class DetectPeopleJobTests : ServiceJobBaseTests<IDetectPeopleService>
    {
        public DetectPeopleJobTests() : base() { }

        [Fact]
        public void Constructor_ConfigNotExist_Exception()
        {
            Assert.Throws<ArgumentNullException>(() => CreateJob(".json"));
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
            Assert.IsType<DetectPeopleConfig>(job.Config);
        }

        [Fact]
        public async Task ExecuteAsync_2FilesFound_StatisticsUpdated()
        {
            var files = new List<MediaFileDto>()
            {
                new (){Date = new (2022,01,01), Name = "File1", Duration = 0},
                new (){Date = new (2022,01,31), Name = "File2", Duration = 0},
            };

            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstanceAsync();
            SetupSaveJobResultAsync();
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<int>(), files))
                .Returns(Task.CompletedTask);
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(files);

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            Assert.Equal(2, job.JobInstance.FilesCount);
            Assert.Equal(DateTime.Today, job.JobInstance.PeriodStart.Value.Date);
            Assert.Equal(DateTime.Today, job.JobInstance.PeriodEnd.Value.Date);
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
            emailMock.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
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

            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>()))
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
            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstanceAsync();
            SetupSaveJobResultAsync();
            SetupExecuteAsync();

            var job = CreateJob();
            await job.ExecuteAsync();

            dbMock.VerifyAll();
        }

        private DetectPeopleJob CreateJob(string configFileName = "ArchiveJobTests.json")
        {
            var config = GetConfig<DetectPeopleConfig>(configFileName);
            return new DetectPeopleJob($"{group}.{triggerKey}", config, serviceMock.Object, dbMock.Object, this.emailMock.Object, this.loggerMock.Object);
        }
    }
}

namespace Job.Tests.Impl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Autofac;
    using Hik.Client.Abstraction;
    using Hik.Client.Infrastructure;
    using Hik.DataAccess.Abstractions;
    using Hik.DataAccess.Data;
    using Hik.DTO.Config;
    using Hik.DTO.Contracts;
    using Job.Email;
    using Job.Impl;
    using Moq;
    using Newtonsoft.Json;
    using Xunit;

    public class ArchiveJobTests
    {
        private const string group = "Test";
        private const string triggerKey = "Key";
        private readonly string CurrentDirectory;
        private readonly Mock<IArchiveService> serviceMock;
        private readonly Mock<IJobService> dbMock;
        private readonly Mock<IEmailHelper> emailMock;

        public ArchiveJobTests()
        {
            serviceMock = new Mock<IArchiveService>(MockBehavior.Strict);
            dbMock = new Mock<IJobService>(MockBehavior.Strict);
            emailMock = new Mock<IEmailHelper>(MockBehavior.Strict);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(serviceMock.Object);

            AppBootstrapper.SetupTest(builder);

            string path = Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().Location).Path);
            CurrentDirectory = Path.GetDirectoryName(path) ?? Environment.ProcessPath ?? Environment.CurrentDirectory;
            CurrentDirectory = Path.Combine(CurrentDirectory, "Configs");
        }

        [Fact]
        public async Task ExecuteAsync_NoFilesFound_NothingSavedToDb()
        {
            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}")).ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>())).Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>())).Returns(Task.CompletedTask);
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue)).ReturnsAsync(new List<MediaFileDTO>());

            var job = new ArchiveJob($"{group}.{triggerKey}", Path.Combine(CurrentDirectory, "ArchiveJobTests.json"), dbMock.Object, this.emailMock.Object, Guid.Empty);
            await job.ExecuteAsync();

            dbMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_2FilesFound_StatisticsUpdated()
        {
            var files = new List<MediaFileDTO>()
            {
                new MediaFileDTO{Date = new DateTime(2022,01,01), Name = "File1", Duration = 0},
                new MediaFileDTO{Date = new DateTime(2022,01,31), Name = "File2", Duration = 0},
            };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}")).ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>())).Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>())).Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), files)).Returns(Task.CompletedTask);
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue)).ReturnsAsync(files);

            var job = new ArchiveJob($"{group}.{triggerKey}", Path.Combine(CurrentDirectory, "ArchiveJobTests.json"), dbMock.Object, this.emailMock.Object, Guid.Empty);
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            Assert.Equal(2, job.JobInstance.FilesCount);
            Assert.Equal(new DateTime(2022, 01, 01), job.JobInstance.PeriodStart);
            Assert.Equal(new DateTime(2022, 01, 31), job.JobInstance.PeriodEnd);
        }

        [Fact]
        public async Task ExecuteAsync_AbnormalActivity_EmailSend()
        {
            var files = new List<MediaFileDTO>()
            {
                new MediaFileDTO(),
                new MediaFileDTO(),
            };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}")).ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>())).Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>())).Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), files)).Returns(Task.CompletedTask);
            emailMock.Setup(x => x.Send($"2 - {group}.{triggerKey}: Abnormal activity detected", It.IsAny<string>())).Verifiable();
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue)).ReturnsAsync(files);

            var job = new ArchiveJob($"{group}.{triggerKey}", Path.Combine(CurrentDirectory, "ArchiveJobTestsAbnormalActivity.json"), dbMock.Object, this.emailMock.Object, Guid.Empty);
            await job.ExecuteAsync();

            dbMock.VerifyAll();
            emailMock.VerifyAll();
        }

        [Fact]
        public async Task ExecuteAsync_VideoFiles_SaveFilesAsync()
        {
            var files = new List<MediaFileDTO>()
            {
                new MediaFileDTO(){ Duration = 1 },
                new MediaFileDTO(){ Duration = 1 },
                new MediaFileDTO(){ Duration = 1 },
            };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}")).ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>())).Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>())).Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>())).ReturnsAsync(new List<MediaFile>());
            dbMock.Setup(x => x.SaveDownloadHistoryFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFile>>())).Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), files)).Returns(Task.CompletedTask);

            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue)).ReturnsAsync(files);

            var job = new ArchiveJob($"{group}.{triggerKey}", Path.Combine(CurrentDirectory, "ArchiveJobTests.json"), dbMock.Object, this.emailMock.Object, Guid.Empty);
            await job.ExecuteAsync();

            dbMock.VerifyAll();
        }

        [Fact]
        public void Constructor_ConfigNotExist_Exception()
        {
            Assert.Throws<FileNotFoundException>(() => new ArchiveJob($"{group}.{triggerKey}", "ArchiveJobTests.json", this.dbMock.Object, this.emailMock.Object, Guid.Empty));
        }

        [Fact]
        public void Constructor_InvalidConfig_Exception()
        {
            Assert.Throws<JsonReaderException>(() => new ArchiveJob($"{group}.{triggerKey}", Path.Combine(this.CurrentDirectory, "ArchiveJobTestsInvalid.json"), dbMock.Object, this.emailMock.Object, Guid.Empty));
        }
    }
}

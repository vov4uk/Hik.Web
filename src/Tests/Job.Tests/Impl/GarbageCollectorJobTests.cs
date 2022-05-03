using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.FileProviders;
using Hik.Client.Infrastructure;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Impl;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Job.Tests.Impl
{
    public class GarbageCollectorJobTests
    {
        protected const string group = "Test";
        protected const string triggerKey = "Key";
        protected readonly Mock<IJobService> dbMock;
        protected readonly Mock<IEmailHelper> emailMock;
        protected readonly Mock<IDirectoryHelper> directoryHelper;
        protected readonly Mock<IFilesHelper> filesHelper;
        protected readonly Mock<IFileProvider> filesProvider;

        public GarbageCollectorJobTests()
        {
            dbMock = new (MockBehavior.Strict);
            emailMock = new (MockBehavior.Strict);
            directoryHelper = new(MockBehavior.Strict);
            filesHelper = new ();
            filesProvider = new (MockBehavior.Strict);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(directoryHelper.Object);
            builder.RegisterInstance(filesHelper.Object);
            builder.RegisterInstance(filesProvider.Object);

            AppBootstrapper.SetupTest(builder);
        }

        [Fact]
        public async Task RunAsync_RetentionPeriodDays_DeleteFilesOlderThan10Days()
        {
            var topFolders = new[] { "C:\\Junk" };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>()))
                .Returns(Task.CompletedTask);
            directoryHelper.Setup(x => x.DeleteEmptyDirs("C:\\Junk"));
            filesProvider.Setup(x => x.Initialize(topFolders))
                .Verifiable();
            filesProvider.Setup(x => x.GetFilesOlderThan("*.*", It.IsAny<DateTime>()))
                .Returns(new List<MediaFileDTO>() { new MediaFileDTO () })
                .Verifiable();
            filesHelper.Setup(x => x.FileSize(It.IsAny<string>())).Returns(0);
            filesHelper.Setup(x => x.DeleteFile(It.IsAny<string>()));

            var job = CreateJob("GCTestsRetention.json");
            await job.ExecuteAsync();
            filesProvider.VerifyAll();
        }

        [Fact]
        public async Task RunAsync_TriggersSetted_DeleteObsoleteJobsAsync()
        {
            var topFolders = new[] { "C:\\Junk" };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.DeleteObsoleteJobsAsync(It.IsAny<string[]>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);
            directoryHelper.Setup(x => x.DeleteEmptyDirs("C:\\Junk"));
            filesProvider.Setup(x => x.Initialize(topFolders))
                .Verifiable();
            filesProvider.Setup(x => x.GetFilesOlderThan("*.*", It.IsAny<DateTime>()))
                .Returns(new List<MediaFileDTO>() {
                    new MediaFileDTO() { Date = new DateTime(2022, 01,01)},
                    new MediaFileDTO() { Date = new DateTime(2022, 01,11)},
                    new MediaFileDTO() { Date = new DateTime(2022, 01,21)},
                    new MediaFileDTO() { Date = new DateTime(2022, 01,31)},
                })
                .Verifiable();
            filesHelper.Setup(x => x.FileSize(It.IsAny<string>())).Returns(0);
            filesHelper.Setup(x => x.DeleteFile(It.IsAny<string>()));

            var job = CreateJob("GCTestsTriggers.json");
            await job.ExecuteAsync();
            filesProvider.VerifyAll();
            Assert.Equal(4, job.JobInstance.FilesCount);
            Assert.Equal(new DateTime(2022, 01, 01), job.JobInstance.PeriodStart);
            Assert.Equal(new DateTime(2022, 01, 31), job.JobInstance.PeriodEnd);
        }

        [Fact]
        public async Task RunAsync_PersentageDelete_GetFilesToDelete2Times()
        {
            var topFolders = new[] { "C:\\FTP\\Floor0" };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>()))
                .Returns(Task.CompletedTask);
            directoryHelper.Setup(x => x.DeleteEmptyDirs("C:\\FTP\\Floor0"));
            filesProvider.Setup(x => x.Initialize(topFolders))
                .Verifiable();
            filesHelper.Setup(x => x.FileSize(It.IsAny<string>())).Returns(0);
            filesHelper.Setup(x => x.DeleteFile(It.IsAny<string>()));

            directoryHelper.Setup(x => x.GetTotalSpaceBytes(It.IsAny<string>())).Returns(100);
            directoryHelper.SetupSequence(x => x.GetTotalFreeSpaceBytes(It.IsAny<string>()))
                .Returns(1)
                .Returns(2)
                .Returns(3);

            filesProvider.Setup(x => x.GetNextBatch(It.IsAny<string>(), 100))
                .Returns(new List<MediaFileDTO>() { new MediaFileDTO() { Date = new DateTime(2022, 01,01)} })
                .Verifiable();

            var job = CreateJob();
            await job.ExecuteAsync();
            filesProvider.VerifyAll();
            dbMock.VerifyAll();
            filesProvider.Verify(x => x.GetNextBatch(It.IsAny<string>(), 100), Times.Exactly(2));
            Assert.Equal(2, job.JobInstance.FilesCount);
        }

        [Fact]
        public async Task RunAsync_PersentageDelete_NoFilesFound()
        {
            var topFolders = new[] { "C:\\FTP\\Floor0" };

            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
            directoryHelper.Setup(x => x.DeleteEmptyDirs("C:\\FTP\\Floor0"));
            filesProvider.Setup(x => x.Initialize(topFolders))
                .Verifiable();

            directoryHelper.Setup(x => x.GetTotalSpaceBytes(It.IsAny<string>())).Returns(100);
            directoryHelper.SetupSequence(x => x.GetTotalFreeSpaceBytes(It.IsAny<string>()))
                .Returns(1)
                .Returns(2)
                .Returns(3);

            filesProvider.Setup(x => x.GetNextBatch(It.IsAny<string>(), 100))
                .Returns(new List<MediaFileDTO>())
                .Verifiable();

            var job = CreateJob();
            await job.ExecuteAsync();
            filesProvider.VerifyAll();
            dbMock.VerifyAll();
            filesProvider.Verify(x => x.GetNextBatch(It.IsAny<string>(), 100), Times.Exactly(1));
            Assert.Equal(0, job.JobInstance.FilesCount);
        }

        [Fact]
        public void Constructor_ValidConfig_ValidConfigType()
        {
            var job = CreateJob();
            Assert.IsType<GarbageCollectorConfig>(job.Config);
        }

        private GarbageCollectorJob CreateJob(string configFileName = "GCTests.json")
            => new GarbageCollectorJob($"{group}.{triggerKey}", Path.Combine(TestsHelper.CurrentDirectory, configFileName), dbMock.Object, this.emailMock.Object, Guid.Empty);

    }
}

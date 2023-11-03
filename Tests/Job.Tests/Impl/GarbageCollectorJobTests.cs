using Hik.Client.FileProviders;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Job.Impl;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Job.Tests.Impl
{
    public class GarbageCollectorJobTests : JobBaseTest
    {
        protected readonly Mock<IDirectoryHelper> directoryHelper;
        protected readonly Mock<IFilesHelper> filesHelper;
        protected readonly Mock<IFileProvider> filesProvider;

        public GarbageCollectorJobTests(ITestOutputHelper output) : base(output)
        {
            directoryHelper = new(MockBehavior.Strict);
            filesHelper = new();
            filesProvider = new(MockBehavior.Strict);
        }

        [Fact]
        public async Task RunAsync_RetentionPeriodDays_DeleteFilesOlderThan10Days()
        {
            var topFolders = new[] { "C:\\Junk" };

            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstance();
            SetupSaveJobResultAsync();
            SetupUpdateDailyStatisticsAsync();
            directoryHelper.Setup(x => x.DeleteEmptyDirs("C:\\Junk"));
            filesProvider.Setup(x => x.Initialize(topFolders))
                .Verifiable();
            filesProvider.Setup(x => x.GetFilesOlderThan(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(new List<MediaFileDto>() { new () })
                .Verifiable();
            filesHelper.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(0);
            filesHelper.Setup(x => x.DeleteFile(It.IsAny<string>()));

            var job = CreateJob("GCTestsRetention.json");
            await job.ExecuteAsync();
            filesProvider.VerifyAll();
        }

        [Fact]
        public async Task RunAsync_TriggersSetted_DeleteObsoleteJobsAsync()
        {
            var topFolders = new[] { "C:\\Junk" };

            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstance();
            SetupSaveJobResultAsync();
            SetupUpdateDailyStatisticsAsync();
            dbMock.Setup(x => x.DeleteObsoleteJobsAsync(It.IsAny<string[]>(), It.IsAny<DateTime>()))
                .Returns(Task.CompletedTask);
            directoryHelper.Setup(x => x.DeleteEmptyDirs("C:\\Junk"));
            filesProvider.Setup(x => x.Initialize(topFolders))
                .Verifiable();
            filesProvider.Setup(x => x.GetFilesOlderThan(It.IsAny<string>(), It.IsAny<DateTime>()))
                .Returns(new List<MediaFileDto>() {
                    new () { Date = new (2022, 01,01)},
                    new () { Date = new (2022, 01,11)},
                    new () { Date = new (2022, 01,21)},
                    new () { Date = new (2022, 01,31)},
                })
                .Verifiable();
            filesHelper.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(0);
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

            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstance();
            SetupSaveJobResultAsync();
            SetupUpdateDailyStatisticsAsync();
            directoryHelper.Setup(x => x.DeleteEmptyDirs("C:\\FTP\\Floor0"));
            filesProvider.Setup(x => x.Initialize(topFolders))
                .Verifiable();
            filesHelper.Setup(x => x.FileSize(It.IsAny<string>()))
                .Returns(0);
            filesHelper.Setup(x => x.DeleteFile(It.IsAny<string>()));

            directoryHelper.Setup(x => x.GetTotalSpaceBytes(It.IsAny<string>()))
                .Returns(100);
            directoryHelper.SetupSequence(x => x.GetTotalFreeSpaceBytes(It.IsAny<string>()))
                .Returns(1)
                .Returns(2)
                .Returns(3);

            filesProvider.Setup(x => x.GetNextBatch(It.IsAny<string>(), 100))
                .Returns(new List<MediaFileDto>() { new MediaFileDto() { Date = new DateTime(2022, 01,01)} })
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

            SetupGetOrCreateJobTriggerAsync();
            SetupCreateJobInstance();
            SetupSaveJobResultAsync();
            directoryHelper.Setup(x => x.DeleteEmptyDirs("C:\\FTP\\Floor0"));
            filesProvider.Setup(x => x.Initialize(topFolders))
                .Verifiable();

            directoryHelper.Setup(x => x.GetTotalSpaceBytes(It.IsAny<string>()))
                .Returns(100);
            directoryHelper.SetupSequence(x => x.GetTotalFreeSpaceBytes(It.IsAny<string>()))
                .Returns(1)
                .Returns(2)
                .Returns(3);

            filesProvider.Setup(x => x.GetNextBatch(It.IsAny<string>(), 100))
                .Returns(new List<MediaFileDto>())
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
        {
            var config = GetConfig(configFileName);
            return new GarbageCollectorJob(new JobTrigger { Group = group, TriggerKey = triggerKey, Config = config }, directoryHelper.Object, filesHelper.Object, filesProvider.Object, dbMock.Object, this.emailMock.Object, this.loggerMock);
        }
    }
}

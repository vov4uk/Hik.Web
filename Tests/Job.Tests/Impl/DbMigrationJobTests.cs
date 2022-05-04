using Autofac;
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
using System.Threading.Tasks;
using Xunit;

namespace Job.Tests.Impl
{
    public class DbMigrationJobTests
    {
        protected const string group = "Test";
        protected const string triggerKey = "Key";
        protected readonly Mock<IHikDatabase> dbMock;
        protected readonly Mock<IEmailHelper> emailMock;
        protected readonly Mock<IFileProvider> filesProvider;

        public DbMigrationJobTests()
        {
            dbMock = new(MockBehavior.Strict);
            emailMock = new(MockBehavior.Strict);
            filesProvider = new(MockBehavior.Strict);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(filesProvider.Object);

            AppBootstrapper.SetupTest(builder);
        }

        [Fact]
        public async Task RunAsync_FoundFiles_FilesSaved()
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
            dbMock.Setup(x => x.SaveFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>()))
                .ReturnsAsync(new List<MediaFile>());
            dbMock.Setup(x => x.SaveDownloadHistoryFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFile>>()))
                .Returns(Task.CompletedTask);

            filesProvider.Setup(x => x.Initialize(topFolders))
                .Verifiable();
            filesProvider.SetupSequence(x => x.GetOldestFilesBatch(false))
                .ReturnsAsync(new List<MediaFileDTO>() {
                    new () { Date = new (2022, 01,01)},
                    new () { Date = new (2022, 01,11)},
                    new () { Date = new (2022, 01,21)},
                    new () { Date = new (2022, 01,31)},
                })
                .ReturnsAsync(new List<MediaFileDTO>() {
                    new () { Date = new (2022, 02,01)},
                    new () { Date = new (2022, 02,11)},
                    new () { Date = new (2022, 02,21)},
                    new () { Date = new (2022, 02,28)},
                })
                .ReturnsAsync(new List<MediaFileDTO>());

            var job = CreateJob();
            await job.ExecuteAsync();
            filesProvider.VerifyAll();
            Assert.Equal(8, job.JobInstance.FilesCount);
        }

        [Fact]
        public void Constructor_ValidConfig_ValidConfigType()
        {
            var job = CreateJob();
            Assert.IsType<MigrationConfig>(job.Config);
        }

        private DbMigrationJob CreateJob(string configFileName = "DBMigrationTests.json")
            => new DbMigrationJob($"{group}.{triggerKey}", Path.Combine(TestsHelper.CurrentDirectory, configFileName), dbMock.Object, this.emailMock.Object, Guid.Empty);

    }
}

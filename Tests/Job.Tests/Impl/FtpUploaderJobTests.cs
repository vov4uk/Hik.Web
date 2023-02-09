using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.Client.Abstraction.Services;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Impl;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Job.Tests.Impl
{
    public class FtpUploaderJobTests : ServiceJobBaseTests<IFtpUploaderService>
    {
        public FtpUploaderJobTests(ITestOutputHelper output) : base(output) { }

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
            dbMock.Setup(x => x.SaveFilesAsync(It.IsAny<HikJob>(), files))
                .ReturnsAsync(new List<MediaFile>());
            dbMock.Setup(x => x.SaveDownloadHistoryFilesAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFile>>()))
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

        private FtpUploaderJob CreateJob(string configFileName = "FtpUploader.json")
        {
            var config = GetConfig<FtpUploaderConfig>(configFileName);
            return new FtpUploaderJob($"{group}.{triggerKey}", config, serviceMock.Object, dbMock.Object, this.emailMock.Object, this.loggerMock);
        }
    }
}

using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Serilog;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Hik.Helpers.Email;

namespace Job.Tests.Impl
{
    public abstract class JobBaseTest
    {
        protected const string group = "Test";
        protected const string triggerKey = "Key";

        protected readonly Mock<IHikDatabase> dbMock;
        protected readonly Mock<IEmailHelper> emailMock;
        protected readonly ILogger loggerMock;

        public JobBaseTest(ITestOutputHelper output)
        {
            dbMock = new (MockBehavior.Strict);
            emailMock = new (MockBehavior.Strict);

            loggerMock = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .WriteTo.TestOutput(output)
                    .CreateLogger();
        }

        protected void SetupUpdateJobTrigger()
        {
            dbMock.Setup(x => x.UpdateJobTrigger(It.IsAny<JobTrigger>()));
        }

        protected void SetupUpdateJob()
        {
            dbMock.Setup(x => x.UpdateJob(It.IsAny<HikJob>()));
        }

        protected void SetupCreateJob()
        {
            dbMock.Setup(x => x.CreateJob(It.IsAny<HikJob>()))
                .Returns(new HikJob() { PeriodEnd = DateTime.MaxValue, PeriodStart = DateTime.MinValue });
        }

        protected void SetupUpdateDailyStatisticsAsync(List<MediaFileDto> files)
        {
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<int>(), files))
                .Returns(Task.CompletedTask);
        }

        protected void SetupUpdateDailyStatisticsAsync()
        {
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<MediaFileDto>>()))
                .Returns(Task.CompletedTask);
        }

        protected void SetupLogExceptionToAsync()
        {
            dbMock.Setup(x => x.LogExceptionTo(It.IsAny<int>(), It.IsAny<string>()));
        }

        protected static string GetConfig(string configFileName)
        {
            return File.ReadAllText(Path.Combine(TestsHelper.CurrentDirectory, configFileName));
        }
    }
}

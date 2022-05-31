using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using Moq;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Job.Tests.Impl
{
    public abstract class JobBaseTest
    {
        protected const string group = "Test";
        protected const string triggerKey = "Key";

        protected readonly Mock<IHikDatabase> dbMock;
        protected readonly Mock<IEmailHelper> emailMock;
        protected readonly Mock<ILogger> loggerMock;

        public JobBaseTest()
        {
            dbMock = new (MockBehavior.Strict);
            emailMock = new (MockBehavior.Strict);
            loggerMock = new ();
        }

        protected void SetupSaveJobResultAsync()
        {
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
        }

        protected void SetupCreateJobInstanceAsync()
        {
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .ReturnsAsync(new HikJob());
        }

        protected void SetupGetOrCreateJobTriggerAsync()
        {
            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
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
            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);
        }

        protected static T GetConfig<T>(string configFileName)
        {
            return HikConfigExtensions.GetConfig<T>(Path.Combine(TestsHelper.CurrentDirectory, configFileName));
        }
    }
}

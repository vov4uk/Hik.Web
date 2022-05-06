using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Job.Email;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Job.Tests.Impl
{
    public abstract class JobBaseTest
    {
        protected const string group = "Test";
        protected const string triggerKey = "Key";

        protected readonly Mock<IHikDatabase> dbMock;
        protected readonly Mock<IEmailHelper> emailMock;

        public JobBaseTest()
        {
            dbMock = new (MockBehavior.Strict);
            emailMock = new (MockBehavior.Strict);
        }

        protected void SetupSaveJobResultAsync()
        {
            dbMock.Setup(x => x.SaveJobResultAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
        }

        protected void SetupCreateJobInstanceAsync()
        {
            dbMock.Setup(x => x.CreateJobInstanceAsync(It.IsAny<HikJob>()))
                .Returns(Task.CompletedTask);
        }

        protected void SetupGetOrCreateJobTriggerAsync()
        {
            dbMock.Setup(x => x.GetOrCreateJobTriggerAsync($"{group}.{triggerKey}"))
                .ReturnsAsync(new JobTrigger());
        }

        protected void SetupUpdateDailyStatisticsAsync(List<MediaFileDTO> files)
        {
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), files))
                .Returns(Task.CompletedTask);
        }

        protected void SetupUpdateDailyStatisticsAsync()
        {
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), It.IsAny<IReadOnlyCollection<MediaFileDTO>>()))
                .Returns(Task.CompletedTask);
        }

        protected void SetupLogExceptionToAsync()
        {
            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);
        }
    }
}

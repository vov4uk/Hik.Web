using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.Infrastructure;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Job.Tests.Impl
{
    public abstract class JobBaseTests<T>
        where T : class, IRecurrentJob
    {
        protected const string group = "Test";
        protected const string triggerKey = "Key";
        
        protected readonly Mock<T> serviceMock;
        protected readonly Mock<IHikDatabase> dbMock;
        protected readonly Mock<IEmailHelper> emailMock;

        public JobBaseTests()
        {
            serviceMock = new (MockBehavior.Strict);
            dbMock = new (MockBehavior.Strict);
            emailMock = new (MockBehavior.Strict);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(serviceMock.Object);

            AppBootstrapper.SetupTest(builder);
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

        protected void SetupExecuteAsync()
        {
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(new List<MediaFileDTO>());
        }

        protected void SetupUpdateDailyStatisticsAsync(List<MediaFileDTO> files)
        {
            dbMock.Setup(x => x.UpdateDailyStatisticsAsync(It.IsAny<HikJob>(), files))
                .Returns(Task.CompletedTask);
        }

        protected void SetupExecuteAsync(List<MediaFileDTO> files)
        {
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(files);
        }

        protected void SetupLogExceptionToAsync()
        {
            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), null))
                .Returns(Task.CompletedTask);
        }
    }
}

using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Job.Tests.Impl
{
    public abstract class JobBaseTest
    {
        protected const string group = "Test";
        protected const string triggerKey = "Key";

        protected readonly Mock<IHikDatabase> dbMock;
        protected readonly Mock<IEmailHelper> emailMock;
        protected readonly ILogger loggerMock;

        private readonly ITestOutputHelper output;

        public JobBaseTest(ITestOutputHelper output)
        {
            this.output = output;
            dbMock = new (MockBehavior.Strict);
            emailMock = new (MockBehavior.Strict);

            var logger = new Mock<ILogger>();
            logger.Setup(logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)
            ))
            .Callback(new InvocationAction(invocation =>
            {
                var logLevel = (LogLevel)invocation.Arguments[0];
                var eventId = (EventId)invocation.Arguments[1];
                var state = invocation.Arguments[2];
                var exception = (Exception?)invocation.Arguments[3];
                var formatter = invocation.Arguments[4];

                var invokeMethod = formatter.GetType().GetMethod("Invoke");
                var actualMessage = (string?)invokeMethod?.Invoke(formatter, new[] { state, exception });

                output.WriteLine(actualMessage);
                if (exception != null)
                {
                    output.WriteLine(exception.ToString());
                }
            }));

            loggerMock = logger.Object;
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
            dbMock.Setup(x => x.LogExceptionToAsync(It.IsAny<int>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        protected static T GetConfig<T>(string configFileName)
        {
            return HikConfigExtensions.GetConfig<T>(Path.Combine(TestsHelper.CurrentDirectory, configFileName));
        }
    }
}

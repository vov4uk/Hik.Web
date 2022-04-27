using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.Infrastructure;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Impl;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Job.Tests.Impl
{
    public class ArchiveJobTests
    {
        private const string group = "Test";
        private const string triggerKey = "Key";
        private readonly string CurrentDirectory;
        private readonly Mock<IBaseRepository<ExceptionLog>> exceptionRepo;
        private readonly Mock<IUnitOfWorkFactory> factoryMock;
        private readonly Mock<IBaseRepository<HikJob>> jobRepo;
        private readonly Mock<IArchiveService> serviceMock;
        private readonly Mock<IBaseRepository<JobTrigger>> triggerRepo;
        private readonly Mock<IUnitOfWork> uowMock;
        public ArchiveJobTests()
        {
            serviceMock = new Mock<IArchiveService>(MockBehavior.Strict);
            factoryMock = new Mock<IUnitOfWorkFactory>(MockBehavior.Strict);
            uowMock = new Mock<IUnitOfWork>(MockBehavior.Strict);
            triggerRepo = new Mock<IBaseRepository<JobTrigger>>(MockBehavior.Strict);
            jobRepo = new Mock<IBaseRepository<HikJob>>(MockBehavior.Strict);
            exceptionRepo = new Mock<IBaseRepository<ExceptionLog>>(MockBehavior.Strict);

            factoryMock.Setup(x => x.CreateUnitOfWork()).Returns(uowMock.Object);
            uowMock.Setup(x => x.GetRepository<JobTrigger>()).Returns(triggerRepo.Object);
            uowMock.Setup(x => x.GetRepository<HikJob>()).Returns(jobRepo.Object);
            uowMock.Setup(x => x.GetRepository<ExceptionLog>()).Returns(exceptionRepo.Object);
            uowMock.Setup(x => x.Dispose());
            uowMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(0);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(serviceMock.Object);

            AppBootstrapper.SetupTest(builder);

            string path = Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().Location).Path);
            CurrentDirectory = Path.GetDirectoryName(path) ?? Environment.ProcessPath ?? Environment.CurrentDirectory;
            CurrentDirectory = Path.Combine(CurrentDirectory, "Configs");
        }

        [Fact]
        public async Task ExecuteAsync_FirstRun_AddNewTriggerToDb()
        {
            JobTrigger empty = default;

            triggerRepo.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>()))
                .ReturnsAsync(empty)
                .Verifiable();
            triggerRepo.Setup(x => x.AddAsync(It.IsAny<JobTrigger>())).Callback<JobTrigger>(
                (x) =>{ empty = x; })
                .ReturnsAsync(new JobTrigger())
                .Verifiable();
            jobRepo.Setup(x => x.AddAsync(It.IsAny<HikJob>())).ReturnsAsync(new HikJob())
                .Verifiable();
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue)).ReturnsAsync(new List<MediaFileDTO>());

            var job = new ArchiveJob($"{group}.{triggerKey}", Path.Combine(CurrentDirectory, "ArchiveJobTests.json"), factoryMock.Object, Guid.Empty);
            await job.ExecuteAsync();

            triggerRepo.VerifyAll();
            jobRepo.VerifyAll();
            uowMock.Verify(x => x.GetRepository<ExceptionLog>(), Times.Never);

            Assert.NotNull(empty);
            Assert.Equal(group, empty.Group);
            Assert.Equal(triggerKey, empty.TriggerKey);
            Assert.True(empty.ShowInSearch);
        }

        [Fact]
        public async Task ExecuteAsync_TriggerExist_GetTriggerFromDb()
        {
            JobTrigger trigger = new JobTrigger() { Id = 1, TriggerKey = triggerKey, Group = group, ShowInSearch = true };

            triggerRepo.Setup(x => x.FindByAsync(It.IsAny<Expression<Func<JobTrigger, bool>>>()))
                .ReturnsAsync(trigger)
                .Verifiable();

            jobRepo.Setup(x => x.AddAsync(It.IsAny<HikJob>())).ReturnsAsync(new HikJob())
                .Verifiable();
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue)).ReturnsAsync(new List<MediaFileDTO>());

            var job = new ArchiveJob($"{group}.{triggerKey}", Path.Combine(CurrentDirectory, "ArchiveJobTests.json"), factoryMock.Object, Guid.Empty);
            await job.ExecuteAsync();

            triggerRepo.VerifyAll();
            triggerRepo.Verify(x => x.AddAsync(It.IsAny<JobTrigger>()), Times.Never);
            jobRepo.VerifyAll();
            uowMock.Verify(x => x.GetRepository<ExceptionLog>(), Times.Never);
        }

        [Fact]
        public void Constructor_ConfigNotExist_Exception()
        {
            Assert.Throws<FileNotFoundException>(() => new ArchiveJob($"{group}.{triggerKey}", "ArchiveJobTests.json", factoryMock.Object, Guid.Empty));
        }

        [Fact]
        public void Constructor_InvalidConfig_Exception()
        {
            Assert.Throws<JsonReaderException>(() => new ArchiveJob($"{group}.{triggerKey}", Path.Combine(CurrentDirectory, "ArchiveJobTestsInvalid.json"), factoryMock.Object, Guid.Empty));
        }
    }
}

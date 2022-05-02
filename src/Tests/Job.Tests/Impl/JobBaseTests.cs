using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.Infrastructure;
using Hik.DataAccess.Abstractions;
using Job.Email;
using Moq;
using System;
using System.IO;
using System.Reflection;

namespace Job.Tests.Impl
{
    public abstract class JobBaseTests<T>
        where T : class, IRecurrentJob
    {
        protected const string group = "Test";
        protected const string triggerKey = "Key";
        protected readonly string CurrentDirectory;
        protected readonly Mock<T> serviceMock;
        protected readonly Mock<IJobService> dbMock;
        protected readonly Mock<IEmailHelper> emailMock;

        public JobBaseTests()
        {
            serviceMock = new Mock<T>(MockBehavior.Strict);
            dbMock = new Mock<IJobService>(MockBehavior.Strict);
            emailMock = new Mock<IEmailHelper>(MockBehavior.Strict);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(serviceMock.Object);

            AppBootstrapper.SetupTest(builder);

            string path = Uri.UnescapeDataString(new UriBuilder(Assembly.GetExecutingAssembly().Location).Path);
            var currentDirectory = Path.GetDirectoryName(path) ?? Environment.ProcessPath ?? Environment.CurrentDirectory;
            CurrentDirectory = Path.Combine(currentDirectory, "Configs");
        }
    }
}

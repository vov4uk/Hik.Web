using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.Infrastructure;
using Hik.DataAccess.Abstractions;
using Job.Email;
using Moq;

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
    }
}

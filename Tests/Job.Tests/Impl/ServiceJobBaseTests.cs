using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.Infrastructure;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Moq;
using System;
using System.Collections.Generic;

namespace Job.Tests.Impl
{
    public abstract class ServiceJobBaseTests<T> : JobBaseTest
        where T : class, IRecurrentJob
    {
        protected readonly Mock<T> serviceMock;

        public ServiceJobBaseTests()
            : base()
        {
            serviceMock = new (MockBehavior.Strict);
            var builder = new ContainerBuilder();
            builder.RegisterInstance(serviceMock.Object);

            AppBootstrapper.SetupTest(builder);
        }

        protected void SetupExecuteAsync()
        {
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(new List<MediaFileDTO>());
        }

        protected void SetupExecuteAsync(List<MediaFileDTO> files)
        {
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(files);
        }
    }
}

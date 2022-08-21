using Hik.Client.Abstraction.Services;
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
        }

        protected void SetupExecuteAsync()
        {
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(new List<MediaFileDto>());
        }

        protected void SetupExecuteAsync(List<MediaFileDto> files)
        {
            serviceMock.Setup(x => x.ExecuteAsync(It.IsAny<BaseConfig>(), DateTime.MinValue, DateTime.MaxValue))
                .ReturnsAsync(files);
        }
    }
}

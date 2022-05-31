using AutoFixture.Xunit2;
using Hik.Helpers.Abstraction;
using Hik.Web.Commands.Config;
using MediatR;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Commands.Tests
{
    public class UpdateQuartzJobConfigCommandHandlerTests
    {
        private readonly Mock<IFilesHelper> fileHelper;

        public UpdateQuartzJobConfigCommandHandlerTests()
        {
            this.fileHelper = new(MockBehavior.Strict);
        }

        [AutoData]
        [Theory]
        public async Task Handle_OverwriteJson(UpdateQuartzJobConfigCommand request)
        {
            fileHelper.Setup(x => x.WriteAllText(request.Path, request.Json));

            var sut = new UpdateQuartzJobConfigCommandHandler(fileHelper.Object);
            var result = await sut.Handle(request, CancellationToken.None);

            Assert.Equal(Unit.Value, result);
        }
    }
}

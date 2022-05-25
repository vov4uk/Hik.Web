using Hik.Web.Pages;
using Hik.Web.Queries.Play;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Tests.Pages
{
    public class PlayModelTests : ModelTestsBase
    {
        [Fact]
        public async Task OnGet_ReturnDto()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<PlayQuery>(), default(CancellationToken)))
                .ReturnsAsync(new PlayDto());

            var sut = new PlayModel(this._mediator.Object);
            var result = await sut.OnGetAsync(1);

            Assert.IsType<PageResult>(result);
            Assert.NotNull(sut.Dto);
        }
    }
}

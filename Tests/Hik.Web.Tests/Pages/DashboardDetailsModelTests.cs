using Hik.DTO.Contracts;
using Hik.Web.Pages;
using Hik.Web.Queries.DashboardDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Tests.Pages
{
    public class DashboardDetailsModelTests : ModelTestsBase
    {
        [Fact]
        public async Task OnGet_ReturnItems()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<DashboardDetailsQuery>(), default(CancellationToken)))
                .ReturnsAsync(new DashboardDetailsDto
                {
                    JobTriggerId = 1,
                    JobTriggerName = name,
                    Items = new DailyStatisticDto[] {new()},
                    TotalItems = 1,
                });

            var sut = new DashboardDetailsModel(this._mediator.Object);
            var result = await sut.OnGetAsync(1, 1);

            Assert.IsType<PageResult>(result);
            Assert.NotNull(sut.Dto);
            Assert.NotEmpty(sut.Dto.Items);
            Assert.Equal(1, sut.Pager.Id);
            Assert.Equal(1, sut.Pager.TotalItems);
            Assert.Equal("?triggerId=", sut.Pager.Url);
        }

        [Fact]
        public async Task OnGet_InvalidTriggerId_ReturnNotFound()
        {
            var sut = new DashboardDetailsModel(this._mediator.Object);
            var result = await sut.OnGetAsync(0, 1);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}

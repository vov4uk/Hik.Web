using Hik.DTO.Contracts;
using Hik.Web.Pages;
using Hik.Web.Queries.JobDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Tests.Pages
{
    public class JobModelDetailsTests : ModelTestsBase
    {
        [Fact]
        public async Task OnGet_ReturnItems()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<JobDetailsQuery>(), default(CancellationToken)))
                .ReturnsAsync(new JobDetailsDto
                {
                    Items = new MediaFileDto[] { new() },
                    TotalItems = 1,
                });

            var sut = new JobDetailsModel(this._mediator.Object);
            var result = await sut.OnGetAsync(1, 1);

            Assert.IsType<PageResult>(result);
            Assert.NotNull(sut.Dto);
            Assert.NotEmpty(sut.Dto.Items);
            Assert.Equal(1, sut.Pager.Id);
            Assert.Equal(1, sut.Pager.TotalItems);
            Assert.Equal("?id=", sut.Pager.Url);
        }

        [Fact]
        public async Task OnGet_InvalidTriggerId_ReturnNotFound()
        {
            var sut = new JobDetailsModel(this._mediator.Object);
            var result = await sut.OnGetAsync(default(int?), 1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task OnGet_JobNotFound_ReturnNotFound()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<JobDetailsQuery>(), default(CancellationToken)))
                .ReturnsAsync(default(JobDetailsDto));

            var sut = new JobDetailsModel(this._mediator.Object);
            var result = await sut.OnGetAsync(1, 1);

            Assert.IsType<NotFoundResult>(result);
        }
    }
}

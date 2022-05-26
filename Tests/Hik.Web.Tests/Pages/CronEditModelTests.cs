using AutoFixture.Xunit2;
using Hik.Web.Commands.Cron;
using Hik.Web.Pages;
using Hik.Web.Queries.QuartzJob;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Tests.Pages
{
    public class CronEditModelTests : ModelTestsBase
    {
        [Fact]
        public void OnGetAddNew_ReturnNewDto()
        {
            var sut = new CronEditModel(this._mediator.Object);
            sut.OnGetAddNew();

            Assert.NotNull(sut.Dto);
            Assert.Null(sut.Dto.Cron.ClassName);
            Assert.Null(sut.Dto.Cron.Group);
            Assert.Null(sut.Dto.Cron.Name);
        }

        [Theory]
        [AutoData]
        public async Task OnGetAsync_ReturnDto(string group, string name)
        {
            this._mediator.Setup(x => x.Send(It.Is<QuartzJobQuery>(y => y.Group == group && y.Name == name), default(CancellationToken)))
                .ReturnsAsync(new QuartzJobDto { Cron = new Quartz.Contracts.CronDto { Name = "Name", ClassName = "Class", Group = "Group"} });
            var sut = new CronEditModel(this._mediator.Object);
            await sut.OnGetAsync(name, group);

            Assert.NotNull(sut.Dto);
            Assert.NotNull(sut.Dto.Cron.ClassName);
            Assert.NotNull(sut.Dto.Cron.Group);
            Assert.NotNull(sut.Dto.Cron.Name);
        }

        [Fact]
        public async Task OnPostAsync_ValidModel_ChangesSaved()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<UpdateQuartzJobCommand>(), default(CancellationToken)))
                .ReturnsAsync(Unit.Value);

            var sut = new CronEditModel(this._mediator.Object);
            sut.OnGetAddNew();
            var result = await sut.OnPostAsync();

            Assert.IsType<RedirectToPageResult>(result);
            var page = (RedirectToPageResult)result;
            Assert.Equal("./Scheduler", page.PageName);
            Assert.Equal("Changes saved. Take effect after Scheduler restart", page.RouteValues.Values.First());
        }

        [Fact]
        public async Task OnPostAsync_InvalidModel_NothingSaved()
        {
            var sut = new CronEditModel(this._mediator.Object);
            sut.ModelState.TryAddModelException("Dto", new System.Exception());
            var result = await sut.OnPostAsync();

            Assert.IsType<PageResult>(result);
        }
    }
}

using AutoFixture.Xunit2;
using Hik.DTO.Contracts;
using Hik.Web.Commands.Cron;
using Hik.Web.Pages;
using Hik.Web.Queries.QuartzTriggers;
using Job;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Tests.Pages
{
    public class SchedulerModelTests : ModelTestsBase
    {
        [Fact]
        public async Task OnGet_ReturnDto()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<QuartzTriggersQuery>(), default(CancellationToken)))
                .ReturnsAsync(new QuartzTriggersDto() { Triggers = new List<TriggerDto>() });

            var sut = new SchedulerModel(this._mediator.Object);
            var result = await sut.OnGetAsync("msg");

            Assert.IsType<PageResult>(result);
            Assert.NotNull(sut.Dto);
            Assert.NotNull(sut.ResponseMsg);
        }

        [Fact]
        public async Task OnPostRestartAsync_Redirect()
        {
            this._mediator.Setup(x => x.Send(It.IsAny<RestartSchedulerCommand>(), default(CancellationToken)))
                .Returns(Task.CompletedTask);

            var sut = new SchedulerModel(this._mediator.Object);
            var result = await sut.OnPostRestartAsync();

            Assert.IsType<RedirectToPageResult>(result);
            var page = (RedirectToPageResult)result;
            Assert.Equal("./Scheduler", page.PageName);
            Assert.Equal("Scheduler restarted", page.RouteValues.Values.First());
        }

        [Theory]
        [AutoData]
        public void OnPostKill_Redirect(string group, string name)
        {
            RunningActivities.Add(new Activity(new Parameters(group, name, ""), null, null, null));

            Assert.NotEmpty(RunningActivities.GetEnumerator());

            var sut = new SchedulerModel(this._mediator.Object);
            var result = sut.OnPostKillAll();

            Assert.IsType<RedirectToPageResult>(result);
            var page = (RedirectToPageResult)result;
            Assert.Equal("./Scheduler", page.PageName);
            Assert.Equal("Jobs stopped", page.RouteValues.Values.First());
            Assert.Empty(RunningActivities.GetEnumerator());
        }
    }
}

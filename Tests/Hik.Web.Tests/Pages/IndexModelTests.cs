using AutoFixture.Xunit2;
using Hik.DTO.Contracts;
using Hik.Web.Commands.Cron;
using Hik.Web.Pages;
using Hik.Web.Queries.QuartzTrigger;
using Hik.Web.Queries.QuartzTriggers;
using Hik.Web.Tests.Pages;
using Job;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Tests
{
    public class IndexModelTests : ModelTestsBase
    {
        [Theory]
        [AutoData]
        public async Task OnPostRun_StartActivity_RedirectToHomePage(string group, string name)
        {
            this._mediator.Setup(x => x.Send(It.IsAny<QuartzTriggerQuery>(), default(CancellationToken)))
                .ReturnsAsync(new QuartzTriggerDto
                {
                    Trigger = new(){ Group = group, Name = name, ClassName = videoJob }
                });

            this._mediator.Setup(x => x.Send(It.Is<StartActivityCommand>(y => y.Group == group && y.Name == name), default(CancellationToken)))
                .Returns(Task.CompletedTask);

            var sut = new IndexModel(this._mediator.Object);
            var result = await sut.OnPostRun(1);

            Assert.IsType<RedirectToPageResult>(result);
            var page = (RedirectToPageResult)result;
            Assert.Equal("./Index", page.PageName);
            Assert.Equal($"Activity {group}.{name} started", page.RouteValues.Values.First());
        }

        [Theory]
        [AutoData]
        public void OnPostKill_ActivityNotFound(string activityId)
        {
            var sut = new IndexModel(this._mediator.Object);
            var result = sut.OnPostKill(activityId);

            Assert.IsType<RedirectToPageResult>(result);
            var page = (RedirectToPageResult)result;
            Assert.Equal("./Index", page.PageName);
            Assert.Equal($"Activity {activityId} not found", page.RouteValues.Values.First());
        }

        [Theory]
        [AutoData]
        public void OnPostKill_ActivityFound(string group, string name)
        {
            RunningActivities.Add(new Activity(new Parameters(group, name, ""), null, null));

            var sut = new IndexModel(this._mediator.Object);
            var result = sut.OnPostKill($"{group}.{name}");

            Assert.IsType<RedirectToPageResult>(result);
            var page = (RedirectToPageResult)result;
            Assert.Equal("./Index", page.PageName);
            Assert.Equal($"Activity { group}.{ name} stopped", page.RouteValues.Values.First());
        }

        [Theory]
        [AutoData]
        public async Task OnGet_ReturnJobs(string msg)
        {
            RunningActivities.Add(new Activity(new Parameters(group, name, ""), null, null));
            this._mediator.Setup(x => x.Send(It.IsAny<QuartzTriggersQuery>(), default(CancellationToken)))
                .ReturnsAsync(new QuartzTriggersDto
                {
                    Triggers = new TriggerDto[]
                    {
                        new(){ Group = group, Name = "Floor0_Video", ClassName = videoJob },
                        new(){ Group = group, Name = name, ClassName = videoJob },
                        new(){ Group = group, Name = "NeverRun", ClassName = archiveJob },
                        new(){ Group = group, Name = "Floor0_Photo", ClassName = photoJob },
                        new(){ Group = group, Name = "Floor1_Photo", ClassName = photoJob },
                    }
                });

            var sut = new IndexModel(this._mediator.Object);
            await sut.OnGet(msg);

            Assert.Equal(msg, sut.ResponseMsg);
            Assert.NotEmpty(sut.TriggersDtos);
            Assert.Single(sut.TriggersDtos.Values.SelectMany(x => x).Where(x => x.ProcessId != null));
            Assert.True(sut.TriggersDtos.ContainsKey(archiveJob));
            Assert.Equal(2, sut.TriggersDtos[videoJob].Count);
            Assert.Equal(2, sut.TriggersDtos[photoJob].Count);
        }
    }
}
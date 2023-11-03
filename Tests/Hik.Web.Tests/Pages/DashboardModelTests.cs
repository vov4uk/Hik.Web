using Hik.DTO.Contracts;
using Hik.Quartz.Contracts;
using Hik.Web.Pages;
using Hik.Web.Queries.Dashboard;
using Hik.Web.Queries.QuartzTriggers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Hik.Web.Tests.Pages
{
    public class DashboardModelTests : ModelTestsBase
    {
        //[Fact]
        //public async Task OnGet_ReturnJobs()
        //{
        //    this._mediator.Setup(x => x.Send(It.IsAny<DashboardQuery>(), default(CancellationToken)))
        //        .ReturnsAsync(new DashboardDto
        //        {
        //            Triggers = new TriggerDto[]
        //            {
        //                new(){ Group = group, Name = "Floor0_Video", LastSync = new(2022,02,24), LastJob = new(){ Success = false } },
        //                new(){ Group = group, Name = name, LastSync = new(2022,02,24), LastJob = new()},
        //                new(){ Group = group, Name = "NeverRun", LastSync = new(2022,02,24) },
        //                new(){ Group = group, Name = "Floor0_Photo", LastSync = new(2022,02,24), LastJob = new()},
        //                new(){ Group = group, Name = "Floor1_Photo", LastSync = new(2022,02,24), LastJob = new()},
        //                new(){ Group = group, Name = "Obsolete", LastSync = new(2022,02,24), LastJob = new()},
        //            },
        //            DailyStatistics = new DailyStatisticDto[]
        //            {
        //                new(), new(), new(),new(), new(), new()
        //            },
        //            Files = new()
        //        });
        //    this._mediator.Setup(x => x.Send(It.IsAny<QuartzTriggersQuery>(), default(CancellationToken)))
        //        .ReturnsAsync(new QuartzTriggersDto
        //        {
        //            Items = new CronDto[]
        //            {
        //                new(){ Group = group, Name = "Floor0_Video", ClassName = videoJob },
        //                new(){ Group = group, Name = name, ClassName = videoJob },
        //                new(){ Group = group, Name = "NeverRun", ClassName = archiveJob },
        //                new(){ Group = group, Name = "Floor0_Photo", ClassName = photoJob },
        //                new(){ Group = group, Name = "Floor1_Photo", ClassName = photoJob },
        //            }
        //        });

        //    var sut = new DashboardModel(this._mediator.Object);
        //    var result = await sut.OnGet();

        //    Assert.IsType<PageResult>(result);
        //    Assert.NotNull(sut.Dto);
        //    Assert.NotEmpty(sut.JobTriggers);
        //    Assert.True(sut.JobTriggers.Values.SelectMany(x => x).All(x => x.Cron == null));
        //    Assert.True(sut.JobTriggers.ContainsKey(archiveJob));
        //    Assert.Equal(2, sut.JobTriggers[videoJob].Count);
        //    Assert.Equal(2, sut.JobTriggers[photoJob].Count);
        //    Assert.Single(sut.JobTriggers[archiveJob]);
        //}
    }
}

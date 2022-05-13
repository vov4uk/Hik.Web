using Hik.Client.Helpers;
using Hik.DTO.Contracts;
using Hik.Web.Queries.Dashboard;
using Hik.Web.Scheduler;
using Job.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly IMediator mediator;

        public DashboardModel(IMediator mediator)
        {
            this.mediator = mediator;
            JobTriggers = new();
        }

        public DashboardDto Dto { get; private set; }
        public Dictionary<string, IList<TriggerDTO>> JobTriggers { get; }
        public async Task<IActionResult> OnGet()
        {
            this.Dto = await mediator.Send(new DashboardQuery()) as DashboardDto;

            var cronTriggers = await QuartzTriggers.GetCronTriggersAsync();
            foreach (var item in cronTriggers)
            {
                var className = item.GetJobClass();
                var group = item.Key.Group;
                var name = item.Key.Name;
                var tri = Dto.Triggers.FirstOrDefault(x => x.Name == name && x.Group == group);
                JobTriggers.SafeAdd(className, tri);
            }

            return Page();
        }
    }
}
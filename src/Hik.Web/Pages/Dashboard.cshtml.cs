using Hik.Helpers;
using Hik.DTO.Contracts;
using Hik.Web.Queries.Dashboard;
using Hik.Web.Queries.QuartzTriggers;
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

            var cronTriggers = await mediator.Send(new QuartzTriggersQuery()) as QuartzTriggersDto;
            foreach (var item in cronTriggers.Items)
            {
                var tri = Dto.Triggers.FirstOrDefault(x => x.Name == item.Name && x.Group == item.Group);
                JobTriggers.SafeAdd(item.ClassName, tri);
            }

            return Page();
        }
    }
}
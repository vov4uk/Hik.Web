using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Hik.Web.Queries.Job;
using Hik.Web.Pages.Shared;

namespace Hik.Web.Pages
{
    public class JobModel : PageModel
    {
        private readonly IMediator mediator;

        public JobModel(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public PagerControl Pager { get; private set; }

        public JobDto Dto { get; set; }

        public async Task<IActionResult> OnGetAsync(int? jobTriggerId = default, int p = 1)
        {
            if (jobTriggerId == null) { return NotFound(); }

            Dto = await mediator.Send(new JobQuery { JobTriggerId = jobTriggerId.Value, CurrentPage = p }) as JobDto;

            if (Dto == null) { return NotFound(); }
            Pager = new (jobTriggerId.Value, "./Job?jobTriggerId=", Dto.TotalItems, p);

            return Page();
        }
    }
}

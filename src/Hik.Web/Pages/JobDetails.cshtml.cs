using Hik.Web.Pages.Shared;
using Hik.Web.Queries.JobDetails;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class JobDetailsModel : PageModel
    {
        private readonly IMediator mediator;

        public JobDetailsModel(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public PagerControl Pager { get; private set; }

        public JobDetailsDto Dto { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int p = 1)
        {
            if (id == null) { return NotFound(); }

            Dto = await mediator.Send(new JobDetailsQuery { JobId = id.Value, CurrentPage = p }) as JobDetailsDto;

            if (Dto == null) { return NotFound(); }

            Pager = new (id.Value, "?id=", Dto.TotalItems, p);

            return Page();
        }
    }
}
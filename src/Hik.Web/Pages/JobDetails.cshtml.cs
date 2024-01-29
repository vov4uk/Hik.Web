using Hik.Web.Pages.Shared;
using Hik.Web.Queries.JobDetails;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

#if USE_AUTHORIZATION
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Security.Claims;
#endif

namespace Hik.Web.Pages
{
#if USE_AUTHORIZATION
    [Authorize(Roles = "Admin,Reader")]
#endif
    public class JobDetailsModel : PageModel
    {
        private readonly IMediator mediator;

        public JobDetailsModel(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public PagerControl Pager { get; private set; }

        public JobDetailsDto Dto { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int p = 1, int pageSize = 48)
        {
            if (id == null) { return NotFound(); }

            Dto = await mediator.Send(new JobDetailsQuery { JobId = id.Value, CurrentPage = p, PageSize = pageSize }) as JobDetailsDto;

            if (Dto == null) { return NotFound(); }
#if USE_AUTHORIZATION
            string allowedTriggers = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.UserData)?.Value;

            if (!string.IsNullOrEmpty(allowedTriggers))
            {
                var triggerIds = allowedTriggers.Split(',').Select(int.Parse).ToList();
                if (!triggerIds.Contains(Dto.Job.JobTriggerId))
                {
                    return RedirectToPage("./Error");
                }
            }
#endif
            Pager = new (id.Value, "?id=", Dto.TotalItems, currentPage: p, pageSize: pageSize);

            return Page();
        }
    }
}
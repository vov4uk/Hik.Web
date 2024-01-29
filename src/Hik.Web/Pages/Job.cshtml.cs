using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Hik.Web.Queries.Job;
using Hik.Web.Pages.Shared;
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
    public class JobModel : PageModel
    {
        private readonly IMediator mediator;

        public JobModel(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public PagerControl Pager { get; private set; }

        public JobDto Dto { get; set; }

        public async Task<IActionResult> OnGetAsync(int? jobTriggerId = default, int p = 1, int pageSize = 48)
        {
            if (jobTriggerId == null) { return NotFound(); }
#if USE_AUTHORIZATION
            string allowedTriggers = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.UserData)?.Value;

            if (!string.IsNullOrEmpty(allowedTriggers) && jobTriggerId.HasValue)
            {
                var triggerIds = allowedTriggers.Split(',').Select(int.Parse).ToList();
                if (!triggerIds.Contains(jobTriggerId.Value))
                {
                    return RedirectToPage("./Error");
                }
            }
#endif
            Dto = await mediator.Send(new JobQuery { JobTriggerId = jobTriggerId.Value, CurrentPage = p, PageSize = pageSize }) as JobDto;

            if (Dto == null) { return NotFound(); }
            Pager = new (jobTriggerId.Value, "./Job?jobTriggerId=", Dto.TotalItems, p, pageSize);

            return Page();
        }
    }
}

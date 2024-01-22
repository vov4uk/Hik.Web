using Hik.DataAccess.Data;
using Hik.Web.Pages.Shared;
using Hik.Web.Queries.JobDetails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    [Authorize(Roles = "Admin,Reader")]
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

            string allowedTriggers = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.UserData)?.Value;

            if (!string.IsNullOrEmpty(allowedTriggers))
            {
                var triggerIds = allowedTriggers.Split(',').Select(int.Parse).ToList();
                if (!triggerIds.Contains(Dto.Job.JobTriggerId))
                {
                    return RedirectToPage("./Error");
                }
            }

            Pager = new (id.Value, "?id=", Dto.TotalItems, currentPage: p);

            return Page();
        }
    }
}
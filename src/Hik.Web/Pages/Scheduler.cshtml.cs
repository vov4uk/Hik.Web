using Hik.Web.Commands.QuartzJob;
using Hik.Web.Queries.QuartzTriggers;
using Job;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class SchedulerModel : PageModel
    {
        public QuartzTriggersDto Dto { get; set; }

        private readonly IMediator _mediator;

        public SchedulerModel(IMediator mediator)
        {
            this._mediator = mediator;
        }

        public async Task<IActionResult> OnGet()
        {
            Dto = await _mediator.Send(new QuartzTriggersQuery()) as QuartzTriggersDto;
            return Page();
        }

        public async Task<IActionResult> OnPostRestartAsync()
        {
            await _mediator.Send(new RestartSchedulerCommand());

            return RedirectToPage("./Index", new { msg = "Scheduler restarted" });
        }

        public IActionResult OnPostKillAll()
        {
            RunningActivities activities = new();
            foreach (var item in activities)
            {
                item?.Kill();
            }
            return RedirectToPage("./Index", new { msg = "Jobs stoped" });
        }
    }
}
using Hik.Web.Commands.Cron;
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
        public string ResponseMsg { get; private set; }

        private readonly IMediator _mediator;

        public SchedulerModel(IMediator mediator)
        {
            this._mediator = mediator;
        }

        public async Task<IActionResult> OnGetAsync(string msg)
        {
            ResponseMsg = msg;
            Dto = await _mediator.Send(new QuartzTriggersQuery()) as QuartzTriggersDto;
            return Page();
        }

        public async Task<IActionResult> OnPostRestartAsync()
        {
            await _mediator.Send(new RestartSchedulerCommand());

            return RedirectToPage("./Scheduler", new { msg = "Scheduler restarted" });
        }

        public async Task<IActionResult> OnPostDeleteAsync(string name, string group, string classname)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await this._mediator.Send(new DeleteQuartzJobCommand { Name = name, Group = group, ClassName = classname });

            return RedirectToPage("./Scheduler", new { msg = "Trigger deleted. Take effect after Scheduler restart" });
        }

        public IActionResult OnPostKillAll()
        {
            RunningActivities activities = new();
            foreach (var item in activities)
            {
                item?.Kill();
            }
            return RedirectToPage("./Scheduler", new { msg = "Jobs stoped" });
        }
    }
}
using Hik.DTO.Contracts;
using Hik.Web.Commands.Cron;
using Hik.Web.Queries.QuartzTriggers;
using Job;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class SchedulerModel : PageModel
    {
        public QuartzTriggersDto Dto { get; set; }

        public Dictionary<string, List<TriggerDto>> Triggers;

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
            Triggers = Dto.Triggers.Where(x => !string.IsNullOrEmpty(x.ClassName)).GroupBy(x => x.ClassName).ToDictionary(k => k.Key, v => v.ToList());
            return Page();
        }

        public async Task<IActionResult> OnPostRestartAsync()
        {
            await _mediator.Send(new RestartSchedulerCommand());

            return RedirectToPage("./Scheduler", new { msg = "Scheduler restarted" });
        }

        public IActionResult OnPostKillAll()
        {
            foreach (var item in RunningActivities.GetEnumerator())
            {
                item?.Kill();
            }
            return RedirectToPage("./Scheduler", new { msg = "Jobs stoped" });
        }
    }
}
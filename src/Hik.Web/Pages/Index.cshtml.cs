using Hik.Helpers;
using Hik.DTO.Contracts;
using Hik.Web.Queries.JobTriggers;
using Hik.Web.Queries.QuartzTriggers;
using Job;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Web.Commands.Cron;

namespace Hik.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly RunningActivities activities = new();
        private readonly IMediator _mediator;

        public IndexModel(IMediator mediator)
        {
            this._mediator = mediator;
        }

        public Dictionary<string, IList<TriggerDto>> TriggersDtos { get; private set; }

        public string ResponseMsg { get; private set; }

        public async Task OnGet(string msg = null)
        {
            ResponseMsg = msg;
            TriggersDtos = new Dictionary<string, IList<TriggerDto>>();

            var triggers = await _mediator.Send(new JobTriggersQuery()) as JobTriggersDto;

            var cronTriggers = await _mediator.Send(new QuartzTriggersQuery()) as QuartzTriggersDto;

            foreach (var cronTrigger in cronTriggers.Items)
            {
                var group = cronTrigger.Group;
                var name = cronTrigger.Name;
                var act = activities.FirstOrDefault(x => x.Parameters.TriggerKey == name && x.Parameters.Group == group);
                var tri = triggers.Items.FirstOrDefault(x => x.Name == name && x.Group == group);

                if (tri == null || tri.LastJob == null)
                {
                    continue;
                }

                tri.ProcessId = act?.ProcessId;
                tri.Cron = cronTrigger;

                TriggersDtos.SafeAdd(cronTrigger.ClassName, tri);
            }
        }

        public IActionResult OnPostRun(string group, string name)
        {
            _mediator.Send(new StartActivityCommand() { Name = name, Group = group}).ConfigureAwait(false).GetAwaiter();

            return RedirectToPage("./Index", new { msg = $"Activity {group}.{name} started" });
        }

        public IActionResult OnPostKill(string activityId)
        {
            var activity = activities.SingleOrDefault(a => a.Id == activityId);
            if (activity != null)
            {
                activity?.Kill();
                return RedirectToPage("./Index", new { msg = $"Activity {activityId} stoped" });
            }
            return RedirectToPage("./Index", new { msg = $"Activity {activityId} not found" });
        }
    }
}
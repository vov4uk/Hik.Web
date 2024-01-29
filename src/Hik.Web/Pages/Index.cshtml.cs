using Hik.Helpers;
using Hik.DTO.Contracts;
using Hik.Web.Queries.QuartzTriggers;
using Job;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Web.Commands.Cron;
using Hik.Web.Queries.QuartzTrigger;
#if USE_AUTHORIZATION
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
#endif

namespace Hik.Web.Pages
{
#if USE_AUTHORIZATION
    [Authorize(Roles = "Admin,Reader")]
#endif
    public class IndexModel : PageModel
    {
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

            var triggers = await _mediator.Send(new QuartzTriggersQuery() { ActiveOnly = true, IncludeLastJob = true }) as QuartzTriggersDto;

            List<TriggerDto> triggersList = triggers.Triggers.ToList();
#if USE_AUTHORIZATION
            string allowedTriggers = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.UserData)?.Value;

            if (!string.IsNullOrEmpty(allowedTriggers))
            {
                var triggerIds = allowedTriggers.Split(',').Select(int.Parse).ToList();
                triggersList = triggersList.Where(x => triggerIds.Contains(x.Id)).ToList();
            }
#endif

            foreach (var cronTrigger in triggersList)
            {
                var act = RunningActivities.GetEnumerator()
                    .FirstOrDefault(x => x.Parameters.TriggerKey == cronTrigger.Name && x.Parameters.Group == cronTrigger.Group);

                cronTrigger.ProcessId = act?.ProcessId;

                TriggersDtos.SafeAdd(cronTrigger.ClassName, cronTrigger);
            }
        }

        public async Task<IActionResult> OnPostRun(int id)
        {
#if USE_AUTHORIZATION
            if (!User.IsInRole("Admin"))
            {
                return RedirectToPage("./Error");
            }
#endif

            var triggerDto = await _mediator.Send(new QuartzTriggerQuery() { Id = id }) as QuartzTriggerDto;
            if (triggerDto?.Trigger != null)
            {
                var trigger = triggerDto.Trigger;
                await _mediator.Send(new StartActivityCommand()
                {
                    Name = trigger.Name,
                    Group = trigger.Group,
                    Environment = Program.Environment,
                    WorkingDirectory = Program.AssemblyDirectory
                });
                return RedirectToPage("./Index", new { msg = $"Activity {trigger.Group}.{trigger.Name} started" });
            }

            return RedirectToPage("./Index", new { msg = $"Failed to start trigger {id}" });
        }

        public IActionResult OnPostKill(string activityId)
        {
#if USE_AUTHORIZATION
            if (!User.IsInRole("Admin"))
            {
                return RedirectToPage("./Error");
            }
#endif

            var activity = RunningActivities.GetEnumerator().SingleOrDefault(a => a.Id == activityId);
            if (activity != null)
            {
                activity?.Kill();
                return RedirectToPage("./Index", new { msg = $"Activity {activityId} stopped" });
            }
            return RedirectToPage("./Index", new { msg = $"Activity {activityId} not found" });
        }
    }
}
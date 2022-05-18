using CronExpressionDescriptor;
using Hik.Client.Helpers;
using Hik.DTO.Contracts;
using Hik.Web.Commands.Activity;
using Hik.Web.Queries.JobTriggers;
using Hik.Web.Scheduler;
using Job;
using Job.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

        public Dictionary<string, IList<TriggerDTO>> TriggersDtos { get; private set; }

        public string ResponseMsg { get; private set; }

        public async Task OnGet(string msg = null)
        {
            ResponseMsg = msg;
            TriggersDtos = new Dictionary<string, IList<TriggerDTO>>();

            var triggers = await this._mediator.Send(new JobTriggersQuery()) as JobTriggersDto;

            IEnumerable<Quartz.Impl.Triggers.CronTriggerImpl> cronTriggers = await QuartzTriggers.GetCronTriggersAsync();

            foreach (var cronTrigger in cronTriggers)
            {
                var className = cronTrigger.GetJobClass();
                var group = cronTrigger.Key.Group;
                var name = cronTrigger.Key.Name;
                var act = activities.FirstOrDefault(x => x.Parameters.TriggerKey == name && x.Parameters.Group == group);
                var tri = triggers.Items.FirstOrDefault(x => x.Name == name && x.Group == group);

                if (tri == null || tri.LastJob == null)
                {
                    continue;
                }

                var ctronTriggerDto = new CronTriggerDto(
                    cronTrigger.GetConfig(),
                    cronTrigger.CronExpressionString,
                    ExpressionDescriptor.GetDescription(cronTrigger.CronExpressionString, CronDTO.CronFormatOptions),
                    cronTrigger.StartTimeUtc.DateTime.ToLocalTime(),
                    cronTrigger.GetNextFireTimeUtc().Value.DateTime.ToLocalTime());

                tri.ProcessId = act?.ProcessId;
                tri.Cron = ctronTriggerDto;

                TriggersDtos.SafeAdd(className, tri);
            }
        }

        public async Task<IActionResult> OnPostRun(string group, string name)
        {
            var cronTriggers = await QuartzTriggers.GetCronTriggersAsync();
            var trigger = cronTriggers.Single(t => t.Key.Group == group && t.Key.Name == name);

            string className = trigger.GetJobClass();
            string configPath = trigger.GetConfig();
            bool runAsTask = Debugger.IsAttached || trigger.GetRunAsTask();

            var parameters = new Parameters(className, group, name, configPath, Program.ConnectionString, runAsTask);

            var command = new ActivityCommand(parameters);
            _mediator.Send(command).ConfigureAwait(false).GetAwaiter();

            return RedirectToPage("./Index", new { msg = $"Activity {group}.{name} started" });
        }

        public IActionResult OnPostKill(string activityId)
        {
            var activity = activities.SingleOrDefault(a => a.Id == activityId);

            activity?.Kill();
            return RedirectToPage("./Index", new { msg = $"Activity {activityId} stoped" });
        }
    }
}
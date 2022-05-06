using Autofac;
using CronExpressionDescriptor;
using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Hik.Web.Scheduler;
using Job;
using Job.Commands;
using Job.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DataContext dataContext;
        private readonly ActivityBag activities = new();
        private readonly IMediator _mediator;

        public IndexModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
            this.dataContext.Database.EnsureCreated();
            _mediator = AutofacConfig.Container.Resolve<IMediator>();
            JobTriggers = dataContext.JobTriggers.AsQueryable().ToList();
        }

        private IList<JobTrigger> JobTriggers { get; }

        public Dictionary<string, IList<TriggerDTO>> TriggersDtos { get; private set; }

        public string ResponseMsg { get; private set; }

        public async Task OnGet(string msg = null)
        {
            ResponseMsg = msg;
            TriggersDtos = new Dictionary<string, IList<TriggerDTO>>();
            var jobs = await dataContext.JobTriggers
                .AsQueryable()
                .Include(x => x.Jobs)
                .Select(x => x.Jobs.OrderByDescending(y => y.Started).FirstOrDefault())
                .ToListAsync();

            IEnumerable<Quartz.Impl.Triggers.CronTriggerImpl> cronTriggers = await QuartzTriggers.GetCronTriggersAsync();

            foreach (var item in cronTriggers)
            {
                var className = item.GetJobClass();
                var group = item.Key.Group;
                var name = item.Key.Name;
                var act = activities.FirstOrDefault(x => x.Parameters.TriggerKey == name && x.Parameters.Group == group);
                var tri = JobTriggers.FirstOrDefault(x => x.TriggerKey == name && x.Group == group);
                var job = jobs.FirstOrDefault(x => x.JobTriggerId == tri?.Id);
                var dto = new TriggerDTO
                {
                    Group = group,
                    Name = name,
                    TriggerStarted = item.StartTimeUtc.DateTime.ToLocalTime(),
                    ConfigPath = item.GetConfig(),
                    Next = item.GetNextFireTimeUtc().Value.DateTime.ToLocalTime(),
                    ActivityId = act?.Id,
                    CronSummary = ExpressionDescriptor.GetDescription(item.CronExpressionString, CronDTO.Options),
                    CronString = item.CronExpressionString,
                    ProcessId = act?.ProcessId,
                    JobTriggerId = tri?.Id ?? -1,
                    JobId = job?.Id,
                    LastSync = tri?.LastSync,
                    Success = job?.Success == true,
                    LastJobPeriodEnd = job?.PeriodEnd,
                    LastJobPeriodStart = job?.PeriodStart,
                    LastJobFilesCount = job?.FilesCount,
                    LastJobStarted = job?.Started,
                    LastJobFinished = job?.Finished
                };

                TriggersDtos.SafeAdd(className, dto);
            }
        }

        public async Task<IActionResult> OnPostRun(string group, string name)
        {
            var cronTriggers = await QuartzTriggers.GetCronTriggersAsync();
            var trigger = cronTriggers.Single(t => t.Key.Group == group && t.Key.Name == name);

            string className = trigger.GetJobClass();
            string configPath = trigger.GetConfig();
            bool runAsTask = Debugger.IsAttached || trigger.GetRunAsTask();

            var configuration = AutofacConfig.Container.Resolve<IConfiguration>();

            IConfigurationSection connStrings = configuration.GetSection("ConnectionStrings");
            string defaultConnection = connStrings.GetSection("HikConnectionString").Value;

            var parameters = new Parameters(className, group, name, configPath, defaultConnection, runAsTask);

            var command = new ActivityCommand(parameters);
            _mediator.Send(command).ConfigureAwait(false).GetAwaiter();

            return RedirectToPage("./Index", new { msg = $"Activity {group}.{name} started" });
        }

        public IActionResult OnPostKill(Guid activityId)
        {
            var activity = activities.SingleOrDefault(a => a.Id == activityId);

            activity?.Kill();
            return RedirectToPage("./Index", new { msg = $"Activity {activityId} dead" });
        }
    }
}
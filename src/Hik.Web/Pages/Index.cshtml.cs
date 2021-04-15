using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using CronExpressionDescriptor;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Job;
using Job.Extentions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;

namespace Hik.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DataContext dataContext;
        private readonly ActivityBag activities = new ActivityBag();
        private readonly Options options = new Options { DayOfWeekStartIndexZero = false, Use24HourTimeFormat = true };

        async public static Task<IList<CronTriggerImpl>> BuildModelAsync()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler("default");
            IReadOnlyCollection<TriggerKey> triggerKeys = await scheduler?.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
            var triggers = triggerKeys?.Select(async t => await scheduler.GetTrigger(t)).ToArray();
            Task.WaitAll(triggers);
            return triggers.Select(x => x.Result as CronTriggerImpl).ToList();
        }

        public IndexModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
            dataContext.Database.EnsureCreated();
            QuartzTriggers = BuildModelAsync().GetAwaiter().GetResult();
            JobTriggers = dataContext.JobTriggers.AsQueryable().ToList();
        }

        public IList<CronTriggerImpl> QuartzTriggers { get; set; }

        public IList<JobTrigger> JobTriggers { get; set; }

        public Dictionary<string, IList<TriggerDTO>> TriggersDTOs { get; set; }

        public string ResponseMsg { get; set; }

        public async Task OnGet(string msg = null)
        {
            ResponseMsg = msg;
            TriggersDTOs = new Dictionary<string, IList<TriggerDTO>>();
            var jobs = await dataContext.JobTriggers
                .AsQueryable()
                .Include(x => x.Jobs)
                .Select(x => x.Jobs.OrderByDescending(y => y.Started)
                .FirstOrDefault())
                .ToListAsync();

            foreach (var item in QuartzTriggers)
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
                    Description = item.Description,
                    TriggerStarted = item.StartTimeUtc.DateTime.ToLocalTime(),
                    ConfigPath = item.GetConfig(),
                    Next = item.GetNextFireTimeUtc().Value.DateTime.ToLocalTime(),
                    ActivityId = act?.Id,
                    CronSummary = ExpressionDescriptor.GetDescription(item.CronExpressionString, options),
                    CronString = item.CronExpressionString,
                    ActivityStarted = act?.StartTime,
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

                TriggersDTOs.SafeAdd(className, dto);

            }
        }

        public IActionResult OnPostRun(string group, string name)
        {
            var trigger = QuartzTriggers.Single(t => t.Key.Group == group && t.Key.Name == name);

            string className = trigger.GetJobClass();
            string configPath = trigger.GetConfig();

            var configuration = AutofacConfig.Container.Resolve<IConfiguration>();

            IConfigurationSection connStrings = configuration.GetSection("ConnectionStrings");
            string defaultConnection = connStrings.GetSection("HikConnectionString").Value;

            var parameters = new Parameters(className, group, name, configPath, defaultConnection);

            var activity = new Activity(parameters);
            Task.Run(() => activity.Start());
            return RedirectToPage("./Index", new { msg = $"Activity {group}.{name} started" });
        }

        public IActionResult OnPostKill(Guid activityId)
        {
            var activity = activities.SingleOrDefault(a => a.Id == activityId);

            if (activity != null)
            {
                activity.Kill();
            }
            return RedirectToPage("./Index", new { msg = $"Activity {activityId} dead" });
        }
    }
}

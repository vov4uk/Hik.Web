using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.Web.Scheduler;
using Job.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly DataContext dataContext;

        public IReadOnlyCollection<DailyStatistic> Statistics { get; private set; }

        public Dictionary<int, DateTime> LatestFiles { get; private set; }

        public Dictionary<string, IList<JobTrigger>> JobTriggers { get; }

        public DashboardModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
            JobTriggers = new Dictionary<string, IList<JobTrigger>>();
        }

        public async Task OnGet()
        {
            var jobTriggers = await this.dataContext.JobTriggers.AsQueryable().ToListAsync();

            Statistics = await this.dataContext.JobTriggers
                .AsQueryable()
                .Include(x => x.DailyStatistics)
                .Select(x => x.DailyStatistics.OrderByDescending(y => y.Period).FirstOrDefault())
                .Where(x => x != null)
                .ToListAsync();

            var latestMediaFiles = await this.dataContext.MediaFiles
                .AsQueryable()
                .GroupBy(x => x.JobTriggerId)
                .Select(x => new KeyValuePair<int, DateTime>(x.Key, x.Max(y => y.Date))).ToListAsync();

            LatestFiles = latestMediaFiles.ToDictionary(x => x.Key, y => y.Value);

            var latestPeriodEnd = await this.dataContext.Jobs
                .AsQueryable()
                .Where(x => x.Finished != null)
                .GroupBy(x => x.JobTriggerId)
                .Select(x => new KeyValuePair<int, DateTime>(x.Key, x.Max(y => y.PeriodEnd ?? new DateTime()))).ToListAsync();

            foreach (var period in latestPeriodEnd)
            {
                if (!LatestFiles.ContainsKey(period.Key))
                {
                    LatestFiles.Add(period.Key, period.Value);
                }
            }

            var cronTriggers = await QuartzTriggers.GetCronTriggersAsync();
            foreach (var item in cronTriggers)
            {
                var className = item.GetJobClass();
                var group = item.Key.Group;
                var name = item.Key.Name;
                var tri = jobTriggers.FirstOrDefault(x => x.TriggerKey == name && x.Group == group);
                JobTriggers.SafeAdd(className, tri);
            }
        }
    }
}
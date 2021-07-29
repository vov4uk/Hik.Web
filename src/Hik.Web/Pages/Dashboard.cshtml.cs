using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Job.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly DataContext dataContext;

        public IReadOnlyCollection<DailyStatistic> Statistics { get; private set; }

        public List<KeyValuePair<int, DateTime>> LatestFiles { get; private set; }

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

            LatestFiles = await this.dataContext.MediaFiles
                .AsQueryable()
                .GroupBy(x => x.JobTriggerId)
                .Select(x => new KeyValuePair<int, DateTime>(x.Key, x.Max(y => y.Date))).ToListAsync();

            var deletedFiles = await this.dataContext.MediaFiles
                .AsQueryable()
                .Include(x => x.DeleteHistory)
                .Where(x => x.DeleteHistory != null)
                .Include("DeleteHistory.Job")
                .Include("DeleteHistory.Job.JobTrigger")
                .GroupBy(x => x.DeleteHistory.Job.JobTriggerId)
                .Select(x => new KeyValuePair<int, DateTime>(x.Key, x.Max(y => y.Date))).ToListAsync();

            LatestFiles.AddRange(deletedFiles);

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
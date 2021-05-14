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
                .Select(x => x.DailyStatistics.OrderByDescending(x=> x.Period).FirstOrDefault())
                .Where(x => x!= null)
                .ToListAsync();

            foreach (var item in QuartzTriggers.Instance)
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
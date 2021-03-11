using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Pages
{
    public class DashboardModel : PageModel
    {
        private readonly DataContext dataContext;

        public IReadOnlyCollection<JobTrigger> Triggers { get; private set; }

        public IReadOnlyCollection<DailyStatistic> Statistics { get; private set; }

        public DashboardModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task OnGet()
        {
            Triggers = await this.dataContext.JobTriggers.ToListAsync();

            var latestStats = await this.dataContext.DailyStatistics
                .GroupBy(x => x.JobTriggerId)
                .Select(x => x.Max(y => y.Id))
                .ToListAsync();
            var last = this.dataContext.DailyStatistics.Where(x => latestStats.Contains(x.Id));            

            Statistics = new List<DailyStatistic>(last);
        }
    }
}
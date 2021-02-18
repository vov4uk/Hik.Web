using System;
using System.Collections.Generic;
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
            var list = new List<DailyStatistic>();
            foreach (var item in Triggers)
            {
                var last = await this.dataContext.DailyStatistics.FirstOrDefaultAsync(
                x => x.JobTriggerId == item.Id && x.Period == (item.LastSync ?? DateTime.Now).Date);
                if (last != null)
                {
                    list.Add(last);
                }
            }

            Statistics = list;
        }
    }
}
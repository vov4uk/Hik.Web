using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using JW;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Pages
{
    public class StatisticModel : PageModel
    {
        private readonly DataContext dataContext;

        public StatisticModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public IList<DailyStatistic> Statistics { get; set; }

        public JobTrigger Trigger { get; set; }

        public Pager Pager { get; set; }

        public int TotalItems { get; set; }

        public int PageSize { get; set; } = 40;

        public int MaxPages { get; set; } = 10;

        public async Task OnGetAsync(int triggerId, int p = 1)
        {
            TotalItems = await dataContext.DailyStatistics.AsQueryable().CountAsync(x => x.JobTriggerId == triggerId);
            Pager = new Pager(TotalItems, p, PageSize, MaxPages);

            var stats = dataContext.DailyStatistics
                .AsQueryable()
                .Where(x => x.JobTriggerId == triggerId)
                .OrderByDescending(x => x.Period)
                .Skip(Math.Max(0, Pager.CurrentPage - 1) * Pager.PageSize)
                .Take(Pager.PageSize);

            Statistics = await stats.ToListAsync();
            Trigger = await dataContext.JobTriggers.FindAsync(triggerId);
        }
    }
}

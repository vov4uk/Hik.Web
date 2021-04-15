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
            Triggers = await this.dataContext.JobTriggers.AsQueryable().ToListAsync();

            Statistics = await this.dataContext.JobTriggers
                .AsQueryable()
                .Include(x => x.DailyStatistics)
                .Select(x => x.DailyStatistics.OrderByDescending(x=> x.Period).FirstOrDefault())
                .Where(x => x!= null)
                .ToListAsync();
        }
    }
}
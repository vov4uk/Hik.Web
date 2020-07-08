using System;
using HikConsole.DataAccess;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using HikConsole.DataAccess.Data;
using System.Linq;
using System.Threading.Tasks;
using JW;
using Microsoft.EntityFrameworkCore;

namespace HikWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly DataContext dataContext;
        const int SECOND = 1;
        const int MINUTE = 60 * SECOND;
        const int HOUR = 60 * MINUTE;
        const int DAY = 24 * HOUR;
        const int MONTH = 30 * DAY;

        public IndexModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
            dataContext.Database.EnsureCreated();
        }

        public IList<HikJob> Jobs { get; set; }

        public Pager Pager { get; set; }
        
        public int TotalItems { get; set; }

        public int PageSize { get; set; } = 40;

        public int MaxPages { get; set; } = 10;

        public async Task OnGetAsync (int p = 1)
        {

            TotalItems = await dataContext.Jobs.CountAsync();
            Pager = new Pager(TotalItems, p, PageSize, MaxPages);

            var repo = dataContext.Jobs.OrderByDescending(x => x.Id).Skip(Math.Max(0, Pager.CurrentPage - 1) * Pager.PageSize).Take(Pager.PageSize);
            Jobs = await repo.ToListAsync();
        }

        public static string GetRelativeTime(DateTime yourDate)
        {
            var ts = new TimeSpan(DateTime.Now.Ticks - yourDate.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            if (delta < 1 * MINUTE)
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

            if (delta < 2 * MINUTE)
                return "a minute ago";

            if (delta < 45 * MINUTE)
                return ts.Minutes + " minutes ago";

            if (delta < 90 * MINUTE)
                return "an hour ago";

            if (delta < 24 * HOUR)
                return ts.Hours + " hours ago";

            if (delta < 48 * HOUR)
                return "yesterday";

            if (delta < 30 * DAY)
                return ts.Days + " days ago";

            if (delta < 12 * MONTH)
            {
                int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }
            else
            {
                int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "one year ago" : years + " years ago";
            }
        }
    }
}

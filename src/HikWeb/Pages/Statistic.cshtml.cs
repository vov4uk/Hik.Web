using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using JW;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HikWeb.Pages
{
    public class StatisticModel : PageModel
    {
        private readonly DataContext dataContext;

        public StatisticModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public IList<DailyStatistic> Statistics { get; set; }

        public Camera Camera { get; set; }

        public Pager Pager { get; set; }

        public int TotalItems { get; set; }

        public int PageSize { get; set; } = 40;

        public int MaxPages { get; set; } = 10;

        public async Task OnGetAsync(int cameraId, int p = 1)
        {
            TotalItems = await dataContext.DailyStatistics.CountAsync(x => x.CameraId == cameraId);
            Pager = new Pager(TotalItems, p, PageSize, MaxPages);

            var stats = dataContext.DailyStatistics
                .Where(x => x.CameraId == cameraId)
                .OrderByDescending(x => x.Period)
                .Skip(Math.Max(0, Pager.CurrentPage - 1) * Pager.PageSize)
                .Take(Pager.PageSize);

            Statistics = await stats.ToListAsync();
            Camera = await dataContext.Cameras.FindAsync(cameraId);
        }
    }
}

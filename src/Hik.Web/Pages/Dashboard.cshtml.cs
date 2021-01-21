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

        public IReadOnlyCollection<Camera> Cameras { get; private set; }

        public IReadOnlyCollection<MediaFile> Files { get; private set; }

        public IReadOnlyCollection<DailyStatistic> Statistics { get; private set; }

        public DashboardModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task OnGet()
        {
            Cameras = await this.dataContext.Cameras.ToListAsync();
            Files = await this.dataContext.Files.FromSqlRaw(@"SELECT *
FROM File
WHERE ID IN (
	SELECT m.ID
	FROM (
	SELECT id
	,      MAX(Date)
	FROM File
	GROUP By CameraId
	) m )").ToListAsync();
            Statistics = await this.dataContext.DailyStatistics.Where(x => x.Period == DateTime.Now.Date).ToListAsync();

        }
    }
}
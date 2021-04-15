using System;
using Hik.DataAccess;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using Hik.DataAccess.Data;
using System.Linq;
using System.Threading.Tasks;
using JW;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Pages
{
    public class JobModel : PageModel
    {
        private readonly DataContext dataContext;
        public JobModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
            dataContext.Database.EnsureCreated();
        }

        public IList<HikJob> Jobs { get; set; }

        public int JobTriggerId { get; set; }

        public Pager Pager { get; set; }

        public int TotalItems { get; set; }

        public int PageSize { get; set; } = 40;

        public int MaxPages { get; set; } = 10;

        public async Task OnGetAsync(int jobTriggerId = default, int p = 1)
        {
            JobTriggerId = jobTriggerId;
            if (jobTriggerId != default)
            {
                TotalItems = await dataContext.Jobs
                    .AsQueryable()
                    .CountAsync(x => x.JobTriggerId == jobTriggerId);
                Pager = new Pager(TotalItems, p, PageSize, MaxPages);

                var repo = dataContext.Jobs
                    .AsQueryable()
                    .Where(x => x.JobTriggerId == jobTriggerId)
                    .Include(x => x.JobTrigger)
                    .OrderByDescending(x => x.Id)
                    .Skip(Math.Max(0, Pager.CurrentPage - 1) * Pager.PageSize)
                    .Take(Pager.PageSize);
                Jobs = await repo.ToListAsync();
            }
        }
    }
}

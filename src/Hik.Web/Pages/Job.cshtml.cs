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
        private const int PageSize = 40;
        private const int MaxPages = 10;
        private readonly DataContext dataContext;
        public JobModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
            dataContext.Database.EnsureCreated();
        }

        public IList<HikJob> Jobs { get; set; }

        public int JobTriggerId { get; private set; }

        public Pager Pager { get; private set; }

        private int TotalItems { get; set; }

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

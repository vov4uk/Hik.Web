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

        public IndexModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
            dataContext.Database.EnsureCreated();
        }

        public IList<HikJob> Jobs { get; set; }

        public Pager Pager { get; set; }
        
        public int TotalItems { get; set; }

        public int PageSize { get; set; } = 25;
        
        public int MaxPages { get; set; }

        public async Task OnGetAsync (int p = 1)
        {

            TotalItems = await dataContext.Jobs.CountAsync();
            MaxPages = TotalItems / PageSize;
            Pager = new Pager(TotalItems, p, PageSize, MaxPages);

            var repo = dataContext.Jobs.OrderByDescending(x => x.Id).Skip(Math.Max(0, Pager.CurrentPage - 1) * Pager.PageSize).Take(Pager.PageSize);
            Jobs = await repo.ToListAsync();
        }
    }
}

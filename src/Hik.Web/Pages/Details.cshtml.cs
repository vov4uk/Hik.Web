using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using JW;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Pages
{
    public class DetailsModel : PageModel
    {
        private readonly DataContext dataContext;

        public DetailsModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public HikJob Job { get; set; }

        public int? JobId { get; set; }

        public IList<MediaFile> Files { get; set; }

        public Pager Pager { get; set; }

        public int TotalItems { get; set; }

        public int PageSize { get; set; } = 40;

        public int MaxPages { get; set; } = 10;

        public async Task<IActionResult> OnGetAsync(int? id, int p = 1)
        {
            if (id == null) { return NotFound(); }
            JobId = id;

            var items = dataContext.Jobs.Where(x => x.Id == id);
            items = items.Include(x => x.ExceptionLog);

            Job = await items.FirstOrDefaultAsync();
            if (Job == null) { return NotFound(); }

            TotalItems = await dataContext.Files.CountAsync(x => x.JobId == id);
            Pager = new Pager(TotalItems, p, PageSize, MaxPages);

            var repo = dataContext.Files
                .Where(x => x.JobId == id)
                .OrderByDescending(x => x.Id)
                .Skip(Math.Max(0, Pager.CurrentPage - 1) * Pager.PageSize)
                .Take(Pager.PageSize);
            Files = await repo.ToListAsync();

            return Page();
        }
    }
}
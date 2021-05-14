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

        public int? JobId { get; private set; }

        public IList<MediaFile> Files { get; private set; }

        public Pager Pager { get; private set; }

        private int TotalItems { get; set; }

        private int PageSize => 40;

        private int MaxPages => 10;

        public async Task<IActionResult> OnGetAsync(int? id, int p = 1)
        {
            if (id == null) { return NotFound(); }
            JobId = id;

            var items = dataContext.Jobs.AsQueryable().Where(x => x.Id == id)
                .Include(x => x.ExceptionLog)
                .Include(x => x.JobTrigger);

            Job = await items.FirstOrDefaultAsync();
            if (Job == null) { return NotFound(); }

            TotalItems = await dataContext.DownloadHistory.AsQueryable().CountAsync(x => x.JobId == id);
            TotalItems += await dataContext.DeleteHistory.AsQueryable().CountAsync(x => x.JobId == id);
            Pager = new Pager(TotalItems, p, PageSize, MaxPages);

            var repo = dataContext.MediaFiles
                .Include(x => x.DownloadHistory)
                .Include(x => x.DeleteHistory)
                .Include(x => x.DownloadDuration)
                .Where(x => (x.DownloadHistory == null ? 0 : x.DownloadHistory.JobId) == id || (x.DeleteHistory == null ? 0 : x.DeleteHistory.JobId) == id)
                .OrderByDescending(x => x.Id)
                .Skip(Math.Max(0, Pager.CurrentPage - 1) * Pager.PageSize)
                .Take(Pager.PageSize);
            Files = await repo.ToListAsync();

            return Page();
        }
    }
}
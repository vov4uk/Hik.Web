using System.Linq;
using System.Threading.Tasks;
using Autofac;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using HikConsole.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace HikWeb.Pages
{
    public class DetailsModel : PageModel
    {
        private readonly DataContext dataContext;

        public DetailsModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public HikJob Job { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, string jobType)
        {
            if (id == null)
            {
                return NotFound();
            }

            var items = dataContext.Jobs.Where(x => x.Id == id);
                
            if(jobType == "HikDownloader")
            {
                items = items.Include(x => x.HardDriveStatuses)
                .Include("HardDriveStatuses.Camera")
                .Include(x => x.Photos)
                .Include(x => x.Videos);
            }
            else
            {
                items = items
                    .Include(x => x.DeletedFiles)
                    .Include("DeletedFiles.Camera");                
            }

            Job = await items.FirstOrDefaultAsync();
            
            return Page();
        }
    }
}
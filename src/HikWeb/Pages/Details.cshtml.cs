using System.Linq;
using System.Threading.Tasks;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
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

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var items = dataContext.Jobs.Where(x => x.Id == id);

            items = items
            .Include(x => x.Photos)
            .Include(x => x.Videos)
            .Include(x => x.DeletedFiles)
            .Include(x => x.ExceptionLog);

            Job = await items.FirstOrDefaultAsync();
            if (Job == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
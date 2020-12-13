using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
            .Include("DeletedFiles.Camera");

            Job = await items.FirstOrDefaultAsync();
            if (Job == null)
            {
                return NotFound();
            }
            return Page();
        }

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];

            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }
    }
}
using HikConsole.DataAccess;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using HikConsole.DataAccess.Data;
using System.Linq;

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

        public void OnGetAsync()
        {
            var repo = dataContext.Jobs.OrderByDescending(x=>x.Id).Take(25);
            Jobs = repo.ToList();
        }
    }
}

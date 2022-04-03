using Hik.Client.Helpers;
using Hik.DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class PlayModel : PageModel
    {
        private readonly DataContext dataContext;
        public PlayModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
            this.dataContext.Database.EnsureCreated();
        }

        public int FileId { get; set; }
        public string Poster { get; set; }

        public async Task OnGetAsync(int fileId)
        {
            FileId = fileId;

            var file = await dataContext.MediaFiles
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.Id == fileId);
            // var path = file.Path;
            var path = @"C:\FFOutput\20220223_151022_151553.mp4";

            var img = await new VideoHelper().GetThumbnailAsync(path);
            Poster = "data:image/jpg;base64," + img;
        }
    }
}

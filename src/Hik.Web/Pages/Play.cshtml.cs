using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DataAccess.Metadata;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Linq;
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
        public string FileTitle { get; set; }
        public string FileFrom { get; set; }
        public string FileTo { get; set; }
        public string Poster { get; set; }
        public MediaFile PreviousFile { get; set; }
        public MediaFile NextFile { get; set; }

        public async Task OnGetAsync(int fileId)
        {
            FileId = fileId;

            var file = await dataContext.MediaFiles
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.Id == fileId);
            var path = file.GetPath();

            if (System.IO.File.Exists(path))
            {
                PreviousFile = await dataContext.MediaFiles
                    .AsQueryable()
                    .Where(x => x.JobTriggerId == file.JobTriggerId && x.Id < file.Id)
                    .OrderByDescending(x => x.Date).FirstOrDefaultAsync();

                NextFile = await dataContext.MediaFiles
                    .AsQueryable()
                    .Where(x => x.JobTriggerId == file.JobTriggerId && x.Id > file.Id)
                    .OrderBy(x => x.Date).FirstOrDefaultAsync();

                var img = await VideoHelper.GetThumbnailAsync(path).ConfigureAwait(false);
                Poster = "data:image/jpg;base64," + img;
                FileTitle = $"{file.Name} ({file.Duration.FormatSeconds()})";
                FileFrom = file.Date.ToString(Consts.DisplayDateTimeStringFormat);
                FileTo = file.Date.AddSeconds(file.Duration ?? 0).ToString(Consts.DisplayDateTimeStringFormat);
            }
            else
            {
                FileTitle = "Not found";
                Poster = "http://vjs.zencdn.net/v/oceans.png";
            }
        }
    }
}
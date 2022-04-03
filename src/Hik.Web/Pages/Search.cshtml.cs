using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class SearchModel : PageModel
    {
        private readonly DataContext dataContext;
        public SearchModel(DataContext dataContext)
        {
            this.dataContext = dataContext;
            this.dataContext.Database.EnsureCreated();
            JobTriggers = dataContext.JobTriggers.AsQueryable().Where(x=>x.ShowInSearch).ToList();
            DateTime = DateTime.Now;
            JobTriggersList = new SelectList(JobTriggers, "Id", "TriggerKey", 1);
        }

        private IList<JobTrigger> JobTriggers { get; }

        public SelectList JobTriggersList { get; set; }
        public List<MediaFile> BeforeFiles { get; private set; }
        public List<MediaFile> Files { get; private set; }
        public List<MediaFile> AfterFiles { get; private set; }

        [BindProperty]
        public int JobTriggerId { get; set; }

        [BindProperty, DataType(DataType.DateTime)]
        public DateTime DateTime { get; set; }

        public async Task OnGetAsync(int? jobTriggerId, DateTime? dateTime)
        {
            Files = new List<MediaFile>();
            if (dateTime == null)
            {
                dateTime = DateTime.Now;
            }
            if (jobTriggerId.HasValue && dateTime.HasValue)
            {
                Files = await dataContext.MediaFiles
                    .AsQueryable()
                    .Where(x => x.JobTriggerId == jobTriggerId && x.Date <= dateTime)
                    .OrderByDescending(x => x.Date).Take(1)
                    .ToListAsync();
                var file = Files.FirstOrDefault();
                if (file != null)
                {
                    BeforeFiles = await dataContext.MediaFiles
                        .AsQueryable()
                        .Where(x => x.JobTriggerId == jobTriggerId && x.Id < file.Id)
                        .OrderByDescending(x => x.Date).Take(5).OrderBy(x => x.Date)
                        .ToListAsync();

                    AfterFiles = await dataContext.MediaFiles
                        .AsQueryable()
                        .Where(x => x.JobTriggerId == jobTriggerId && x.Id > file.Id)
                        .OrderBy(x => x.Date).Take(5)
                        .ToListAsync();
                }
            }
        }

        public async Task<FileResult> OnGetDownloadFile(int fileId)
        {

            var file = await dataContext.MediaFiles
                    .AsQueryable()
                    .FirstOrDefaultAsync(x => x.Id == fileId);

            // For debug
            file.Path = @"C:\FFOutput\20220223_151022_151553.mp4";

            byte[] bytes = System.IO.File.ReadAllBytes(file.Path);
            return File(bytes, "application/octet-stream", Path.GetFileName(file.Path));
        }

        public async Task<IActionResult> OnGetStreamFile(int fileId)
        {
            var file = await dataContext.MediaFiles
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.Id == fileId);
            var path = @"C:\FFOutput\20220223_151022_151553.mp4";

            //Build the File Path.
            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return new FileStreamResult(memory, new MediaTypeHeaderValue("video/mp4")) { EnableRangeProcessing = true };
        }
    }
}

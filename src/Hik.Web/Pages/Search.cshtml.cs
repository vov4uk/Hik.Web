using Hik.DataAccess;
using Hik.DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            JobTriggers = dataContext.JobTriggers.AsQueryable().ToList();
           // DateTime = DateTime.Now;
            JobTriggersList = new SelectList(JobTriggers, "Id", "TriggerKey", 1);
        }

        private IList<JobTrigger> JobTriggers { get; }

        public SelectList JobTriggersList { get; set; }
        public List<MediaFile> BeforeFiles { get; private set; }
        public List<MediaFile> Files { get; private set; }
        public List<MediaFile> AfterFiles { get; private set; }

        [BindProperty]
        public int JobTriggerId { get; set; }

        [BindProperty, DataType(DataType.DateTime), DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
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
    }
}

using Hik.Web.Queries.FilePath;
using Hik.Web.Queries.Search;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Net.Http.Headers;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class SearchModel : PageModel
    {
        private readonly IMediator mediator;

        public SearchModel(IMediator mediator)
        {
            this.mediator = mediator;

            var jobTriggers = mediator.Send(new SearchTriggersQuery()).GetAwaiter().GetResult() as SearchTriggersDto;
            DateTime = DateTime.Now;
            JobTriggersList = new SelectList(jobTriggers.Triggers, "Key", "Value", 1);
        }

        public SelectList JobTriggersList { get; set; }

        public SearchDto Dto { get; private set; }

        [BindProperty]
        public int JobTriggerId { get; set; }

        [BindProperty, DataType(DataType.DateTime)]
        public DateTime DateTime { get; set; }

        public async Task<IActionResult> OnGetAsync(int? jobTriggerId, DateTime? dateTime)
        {
            if (dateTime == null)
            {
                dateTime = DateTime.Now;
            }

            dateTime = new DateTime(dateTime.Value.Year, dateTime.Value.Month, dateTime.Value.Day, dateTime.Value.Hour, dateTime.Value.Minute, 0, 0);
            if (jobTriggerId.HasValue && dateTime.HasValue)
            {
                Dto = await mediator.Send(new SearchQuery() { DateTime = dateTime, JobTriggerId = jobTriggerId.Value}) as SearchDto;
            }

            return Page();
        }

        public async Task<IActionResult> OnGetDownloadFile(int fileId)
        {
            var file = await mediator.Send(new FilePathQuery() { FileId = fileId }) as FilePathDto;

            if (file == null || !System.IO.File.Exists(file.Path))
            {
                return NotFound();
            }

            byte[] bytes = await System.IO.File.ReadAllBytesAsync(file.Path);
            return File(bytes, "application/octet-stream", Path.GetFileName(file.Path));
        }

        public async Task<IActionResult> OnGetStreamFile(int fileId)
        {
            var file = await mediator.Send(new FilePathQuery() { FileId = fileId }) as FilePathDto;

            if (file == null || !System.IO.File.Exists(file.Path))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(file.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 65536, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return new FileStreamResult(memory, new MediaTypeHeaderValue("video/mp4")) { EnableRangeProcessing = true };
        }
    }
}
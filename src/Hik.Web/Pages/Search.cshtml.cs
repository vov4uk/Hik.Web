using Hik.Helpers.Abstraction;
using Hik.Web.Queries.FilePath;
using Hik.Web.Queries.Photo;
using Hik.Web.Queries.Search;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Net.Http.Headers;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class SearchModel : PageModel
    {
        private readonly IMediator mediator;
        private readonly IFilesHelper filesHelper;

        public SearchModel(IMediator mediator, IFilesHelper filesHelper)
        {
            this.mediator = mediator;
            this.filesHelper = filesHelper;

            DateTime = DateTime.Now;
            var jobTriggers = mediator.Send(new SearchTriggersQuery()).GetAwaiter().GetResult() as SearchTriggersDto;
            JobTriggersList = new SelectList(jobTriggers.Triggers, "Key", "Value", 1);
        }

        public SelectList JobTriggersList { get; private set; }

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
            if (jobTriggerId.HasValue)
            {
                Dto = await mediator.Send(new SearchQuery() { DateTime = dateTime, JobTriggerId = jobTriggerId.Value}) as SearchDto;
            }

            return Page();
        }

        public async Task<IActionResult> OnGetDownloadFile(int fileId)
        {
            var filePath = await GetFilePath(fileId);

            if (string.IsNullOrEmpty(filePath))
            {
                return NotFound();
            }

            byte[] bytes = await filesHelper.ReadAllBytesAsync(filePath);
            return File(bytes, "application/octet-stream", filesHelper.GetFileName(filePath));
        }

        public async Task<IActionResult> OnGetStreamFile(int fileId)
        {
            var filePath = await GetFilePath(fileId);

            if (string.IsNullOrEmpty(filePath))
            {
                return NotFound();
            }

            var memory = await filesHelper.ReadAsMemoryStreamAsync(filePath);
            return new FileStreamResult(memory, new MediaTypeHeaderValue("video/mp4")) { EnableRangeProcessing = true };
        }

        public async Task<IActionResult> OnGetImage(int fileId)
        {
            var filePath = await GetFilePath(fileId);

            if (string.IsNullOrEmpty(filePath))
            {
                return NotFound();
            }

            return base.PhysicalFile(filePath, "image/jpg");
        }

        public async Task<IActionResult> OnGetImageThumbnail(int fileId)
        {
            var thumbnail = await mediator.Send(new PhotoThumbnailQuery() { FileId = fileId }) as PhotoThumbnailDto;
            return File(thumbnail.Poster, "image/jpg");
        }

        private async Task<string> GetFilePath(int fileId)
        {
            var file = await mediator.Send(new FilePathQuery() { FileId = fileId }) as FilePathDto;

            if (file == null || !filesHelper.FileExists(file.Path))
            {
                return null;
            }
            return file.Path;
        }
    }
}
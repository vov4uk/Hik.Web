using Hik.Web.Queries.JobDetails;
using JW;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class DetailsModel : PageModel
    {
        private const int PageSize = 40;
        private const int MaxPages = 10;
        private readonly IMediator mediator;

        public DetailsModel(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public int? JobId { get; private set; }

        public Pager Pager { get; private set; }

        public JobDetailsDto JobDetailsDto { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int p = 1)
        {
            if (id == null) { return NotFound(); }
            JobId = id;

            JobDetailsDto = await mediator.Send(new JobDetailsQuery { JobId = id.Value, CurrentPage = p, MaxPages = MaxPages, PageSize = PageSize }) as JobDetailsDto;

            if (JobDetailsDto == null) { return NotFound(); }
            Pager = new Pager(JobDetailsDto.TotalItems, p, PageSize, MaxPages);

            return Page();
        }
    }
}
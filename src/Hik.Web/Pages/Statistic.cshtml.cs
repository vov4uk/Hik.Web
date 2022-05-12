using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.Web.Queries.Statistic;
using JW;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class StatisticModel : PageModel
    {
        private const int PageSize = 40;
        private const int MaxPages = 10;
        private readonly IMediator mediator;

        public StatisticModel(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public Pager Pager { get; set; }

        public StatisticDto Dto { get; set; }

        public async Task<IActionResult> OnGetAsync(int triggerId, int p = 1)
        {
            if (triggerId <= 0) { return NotFound(); }
            Dto = await mediator.Send(new StatisticQuery { TriggerId = triggerId, CurrentPage = p, MaxPages = MaxPages, PageSize = PageSize }) as StatisticDto;

            Pager = new Pager(Dto.TotalItems, p, PageSize, MaxPages);
            return Page();
        }
    }
}
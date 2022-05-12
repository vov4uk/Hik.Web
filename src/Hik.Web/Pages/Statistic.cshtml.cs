using Hik.Web.Pages.Shared;
using Hik.Web.Queries.Statistic;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class StatisticModel : PageModel
    {
        private readonly IMediator mediator;

        public StatisticModel(IMediator mediator)
        {
            this.mediator = mediator;
        }

        public PagerControl Pager { get; set; }

        public StatisticDto Dto { get; set; }

        public async Task<IActionResult> OnGetAsync(int triggerId, int p = 1)
        {
            if (triggerId <= 0) { return NotFound(); }
            Dto = await mediator.Send(new StatisticQuery { TriggerId = triggerId, CurrentPage = p}) as StatisticDto;

            Pager = new (triggerId, "?triggerId=", Dto.TotalItems, p);
            return Page();
        }
    }
}
using Hik.Quartz.Contracts;
using Hik.Web.Commands.QuartzJob;
using Hik.Web.Queries.QuartzJob;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class CronEditModel : PageModel
    {
        private readonly IMediator _mediator;

        public CronEditModel(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [BindProperty]
        public QuartzJobDto Dto { get; set; }

        public void OnGetAddNew()
        {
            Dto = new QuartzJobDto() { Cron = new CronDto()};
        }

        public async Task OnGetAsync(string name, string group)
        {
            Dto = await this._mediator.Send(new QuartzJobQuery { Name = name, Group = group }) as QuartzJobDto;
        }

        public async Task <IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await this._mediator.Send(new UpdateQuartzJobCommand { Cron = Dto.Cron });

            return RedirectToPage("./Scheduler");
        }
    }
}
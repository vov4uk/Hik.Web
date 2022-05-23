using Hik.Web.Queries.QuartzJobConfig;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Threading.Tasks;

namespace Hik.Web.Pages
{
    public class ConfigEditModel : PageModel
    {
        private readonly IMediator _mediator;

        public ConfigEditModel(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [BindProperty]
        public QuartzJobConfigDto Dto { get; set; }

        public async Task OnGetAsync(string name, string group)
        {
            Dto = await this._mediator.Send(new QuartzJobConfigQuery { Name = name, Group = group }) as QuartzJobConfigDto;
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var json = (ModelState["Dto.ConfigDTO.Json"].RawValue as string[])?.Last();

            System.IO.File.WriteAllText(Dto.ConfigDTO.GetConfigPath(), json);
            return RedirectToPage("./Scheduler");
        }
    }
}

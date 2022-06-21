using Hik.Web.Commands.Config;
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
        private const string JsonKey = "Dto.Config.Json";
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

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var jsonArray = ModelState[JsonKey].RawValue as string[];
                string json;
                if (jsonArray != null)
                {
                    json = jsonArray.Last();
                }
                else
                {
                    json = ModelState[JsonKey].RawValue as string;
                }

                await this._mediator.Send(new UpdateQuartzJobConfigCommand { Path = Dto.Config.GetConfigPath(), Json = json });
                return RedirectToPage("./Scheduler", new { msg = "Changes saved" });
            }

            return RedirectToPage("./Scheduler", new { msg = "Model invalid" });
        }
    }
}

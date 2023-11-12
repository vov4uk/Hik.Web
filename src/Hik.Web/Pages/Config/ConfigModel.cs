using Hik.DTO.Config;
using Hik.Web.Commands.Cron;
using Hik.Web.Queries.QuartzTrigger;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Hik.Web.Pages.Config
{
    public abstract class ConfigModel<T> : PageModel
        where T : BaseConfig, new()
    {
        protected readonly IMediator _mediator;

        [BindProperty]
        public int Id { get; set; }

        [BindProperty]
        public T Config { get; set; }

        public ConfigModel(IMediator mediator)
        {
            _mediator = mediator;
        }

        public virtual async Task OnGetAsync(int id)
        {
            var Dto = (await _mediator.Send(new QuartzTriggerQuery { Id = id }) as QuartzTriggerDto)?.Trigger;
            if (Dto != null)
            {
                Config = string.IsNullOrEmpty(Dto.Config) ? new T() : JsonConvert.DeserializeObject<T>(Dto.Config);
                Id = id;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await this._mediator.Send(new UpdateTriggerConfigCommand { TriggerId = Id, JsonConfig = JsonConvert.SerializeObject(Config, Formatting.Indented) });

            return RedirectToPage("/Scheduler", new { msg = "Changes saved. Take effect on next job run" });
        }
    }

}

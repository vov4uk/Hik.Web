using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.DTO.Config;
using Hik.Web.Queries.QuartzTriggers;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hik.Web.Pages.Config
{
    public class GCModel : ConfigModel<GarbageCollectorConfig>
    {
        public List<SelectListItem> Triggers { get; private set; }

        public GCModel(IMediator mediator) : base(mediator) { }

        public override async Task OnGetAsync(int id)
        {
            await base.OnGetAsync(id);

            var triggers = await _mediator.Send(new QuartzTriggersQuery() { ActiveOnly = true, IncludeLastJob = false }) as QuartzTriggersDto;

            Triggers = triggers.Triggers.Select(x => new SelectListItem
            {
                Text = x.Name,
                Value = x.Id.ToString(),
                Selected = base.Config.Triggers.Contains(x.Id)
            }).ToList();
        }
    }
}

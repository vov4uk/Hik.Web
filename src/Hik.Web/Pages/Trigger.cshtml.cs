using Hik.Web.Commands.Cron;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using Job.Impl;
using Job.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Hik.Web.Queries.QuartzTrigger;
using Hik.DTO.Contracts;

namespace Hik.Web.Pages
{
    public class TriggerModel : PageModel
    {
        private static List<Type> jobTypes;
        private static Dictionary<string, Type> configTypes;

        private readonly IMediator _mediator;

        public static List<SelectListItem> JobTypesList { get; private set; }

        static TriggerModel()
        {
            var baseClass = typeof(JobProcessBase<>);
            jobTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(x => x.IsClass && !x.IsAbstract && x.IsInheritedFrom(baseClass))
                .ToList();

            configTypes = jobTypes.ToDictionary(k => k.FullName, v => v.BaseType.GenericTypeArguments[0]);

            JobTypesList = jobTypes.Select(x => new SelectListItem { Text = x.Name, Value = x.Name } ).ToList();
        }

        public TriggerModel(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [BindProperty]
        public TriggerDto Dto { get; set; }

        public void OnGetAddNew()
        {
            Dto = new TriggerDto();
        }

        public async Task OnGetAsync(int id)
        {
            Dto =(await this._mediator.Send(new QuartzTriggerQuery { Id = id }) as QuartzTriggerDto)?.Trigger;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await this._mediator.Send(new UpsertTriggerCommand { Trigger = Dto });

            return RedirectToPage("./Scheduler", new { msg = "Changes saved. Take effect after Scheduler restart" });
        }
    }
}

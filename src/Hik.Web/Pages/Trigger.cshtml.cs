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
using Hik.Quartz.Contracts.Xml;

namespace Hik.Web.Pages
{
    public class TriggerModel : PageModel
    {
        private static List<Type> jobTypes;
        public static Dictionary<string, Type> ConfigTypes;

        private readonly IMediator _mediator;

        public static List<SelectListItem> JobTypesList { get; private set; }

        static TriggerModel()
        {
            var baseClass = typeof(JobProcessBase<>);
            jobTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(x => x.IsClass && !x.IsAbstract && x.IsInheritedFrom(baseClass))
                .ToList();

            ConfigTypes = jobTypes.ToDictionary(k => k.FullName, v => v.BaseType.GenericTypeArguments[0]);

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

            var id = await this._mediator.Send(new UpsertTriggerCommand { Trigger = Dto });

            if (Dto.Id == 0)
            {
                string configType = "";
                switch (Dto.ClassName)
                {
                    case "GarbageCollectorJob": configType = "GC"; break;
                    case "ArchiveJob": configType = "Archive"; break;
                    default: configType = "Camera"; break;
                }

                return RedirectToPage($"./Config/{configType}", new { id = id });
            }

            return RedirectToPage("./Scheduler", new { msg = "Changes saved. Take effect after Scheduler restart" });
        }
    }
}

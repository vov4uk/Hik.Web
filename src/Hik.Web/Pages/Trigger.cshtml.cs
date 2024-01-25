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
using Newtonsoft.Json;

namespace Hik.Web.Pages
{
    public class TriggerModel : PageModel
    {
        private static List<Type> jobTypes;
        public static Dictionary<string, string> ConfigTypes;

        private readonly IMediator _mediator;

        public static List<SelectListItem> JobTypesList { get; private set; }

        static TriggerModel()
        {
            var baseClass = typeof(JobProcessBase<>);
            jobTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(x => x.IsClass && !x.IsAbstract && x.IsInheritedFrom(baseClass))
                .ToList();

            ConfigTypes = jobTypes.ToDictionary(k => k.Name, v => JsonConvert.SerializeObject(Activator.CreateInstance(v.BaseType.GenericTypeArguments[0]), Formatting.Indented));

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

        public IActionResult OnGetConfigJson(string classid)
        {
            return Content(ConfigTypes[classid]);
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
                string configType;
                switch (Dto.ClassName)
                {
                    case "GarbageCollectorJob": configType = "GC"; break;
                    case "FilesCollectorJob":  configType = "FilesCollector"; break;
                    case "ImagesCollectorJob": configType = "ImagesCollector"; break;
                    case "VideoDownloaderJob": configType = "Camera"; break;
                    case "PhotoDownloaderJob": configType = "Camera"; break;
                    default: return RedirectToPage("./Scheduler", new { msg = "Invalid className" });
                }

                return RedirectToPage($"./Config/{configType}", new { id });
            }

            return RedirectToPage("./Scheduler", new { msg = "Changes saved. Take effect after Scheduler restart" });
        }
    }
}

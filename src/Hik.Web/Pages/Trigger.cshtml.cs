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
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Serilog;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Hik.DTO.Config;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;

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
            Dto = new TriggerDto();
        }

        public ActionResult OnPostSubmit([FromBody] TriggerDto dto)
        {
            return RedirectToAction("Index");
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

        public PartialViewResult OnPostConfigView(string classid, [FromBody]TriggerModel dto)
        {
            string view = "_CameraJob";
            if (classid == "ArchiveJob")
            {
                view = "_ArchiveJob";
            }
            else if (classid == "GarbageCollectorJob")
            {
                view = "_GCJob";
            }

            return Partial(view, dto);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return Page();
            //if (!IsValidJson(Dto.Config) || !ModelState.IsValid)
            //{
            //    return Page();
            //}

            //await this._mediator.Send(new UpsertTriggerCommand { Trigger = Dto });

            //return RedirectToPage("./Scheduler", new { msg = "Changes saved. Take effect after Scheduler restart" });
        }

        private static bool IsValidJson(string strInput)
        {
            if (string.IsNullOrWhiteSpace(strInput)) { return false; }
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Log.Error(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Log.Error(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}

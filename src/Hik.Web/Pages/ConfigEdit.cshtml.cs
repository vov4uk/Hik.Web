using Hik.Web.Scheduler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;
using System.Text.Json;

namespace Hik.Web.Pages
{
    public class ConfigEditModel : PageModel
    {
        [BindProperty]
        public ConfigDTO ConfigDTO { get; set; }

        public void OnGet(string name, string group)
        {
            var data = XmlHelper.GetJobSchedulingData();

            if (data.Schedule.Trigger.Any())
            {
                var cron = data.Schedule.Trigger.Select(x => x.Cron).FirstOrDefault(x => x.Group == group && x.Name == name);
                if (cron != null)
                {
                    ConfigDTO = new ConfigDTO(cron);
                    if (System.IO.File.Exists(ConfigDTO.Path))
                    {
                        ConfigDTO.Json = PrettyJson(System.IO.File.ReadAllText(ConfigDTO.Path));
                    }
                    else
                    {
                        var stream = System.IO.File.Create(ConfigDTO.Path);
                        stream.Dispose();
                    }
                }
            }
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var json = (ModelState["ConfigDTO.Json"].RawValue as string[])?.Last();

            System.IO.File.WriteAllText(ConfigDTO.Path, json);
            return RedirectToPage("./Scheduler");
        }

        private static string PrettyJson(string unPrettyJson)
        {
            if(string.IsNullOrEmpty(unPrettyJson))
                return string.Empty;

            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };

            var jsonElement = JsonSerializer.Deserialize<JsonElement>(unPrettyJson);

            return JsonSerializer.Serialize(jsonElement, options);
        }
    }
}

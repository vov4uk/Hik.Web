using Hik.Web.Scheduler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace Hik.Web.Pages
{
    public class CronEditModel : PageModel
    {
        [BindProperty]
        public CronDTO CronDTO { get; set; }

        public void OnGet(string name, string group)
        {
            var data = XmlHelper.GetJobSchedulingData();

            if (data.Schedule.Trigger.Any())
            {
                var cron = data.Schedule.Trigger.Select(x => x.Cron).FirstOrDefault(x => x.Group == group && x.Name == name);
                CronDTO = new CronDTO(cron);
            }
        }

        public IActionResult OnPost(string name, string group)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var data = XmlHelper.GetJobSchedulingData();

            var modified = CronDTO.ToCron();

            var original = data.Schedule.Trigger.FirstOrDefault(x => x.Cron.Group == group && x.Cron.Name == name);
            data.Schedule.Trigger.Remove(original);
            data.Schedule.Trigger.Add(new Trigger { Cron = modified });

            XmlHelper.UpdateJobSchedulingData(data);

            return RedirectToPage("./Scheduler");
        }
    }
}
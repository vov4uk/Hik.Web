using Hik.Web.Scheduler;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Hik.Web.Pages
{
    public class CronAddModel : PageModel
    {
        [BindProperty]
        public CronDTO CronDTO { get; set; }

        public void OnGet()
        {
            CronDTO = new CronDTO();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var data = XmlHelper.GetJobSchedulingData();

            var newItem = CronDTO.ToCron();

            data.Schedule.Trigger.Add(new Trigger { Cron = newItem });

            XmlHelper.UpdateJobSchedulingData(data);

            return RedirectToPage("./Scheduler");
        }
    }
}

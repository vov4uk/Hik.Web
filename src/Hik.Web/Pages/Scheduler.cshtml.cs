using Autofac;
using Hik.Web.Scheduler;
using Job;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Quartz;
using Quartz.Impl;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Hik.Web.Pages
{
    public class SchedulerModel : PageModel
    {
        public List<CronDTO> Crons { get; set; } = new List<CronDTO>();
        public JobSchedulingData Data { get; set; }

        public void OnGet()
        {
            var configuration = AutofacConfig.Container.Resolve<IConfiguration>();
            var options = new QuartzOption(configuration);
            var xmlFilePath = options.Plugin.JobInitializer.FileNames;
            var xml = System.IO.File.ReadAllText(xmlFilePath);

            XmlSerializer serializer = new XmlSerializer(typeof(JobSchedulingData));
            using (StringReader reader = new StringReader(xml))
            {
                Data = (JobSchedulingData)serializer.Deserialize(reader);
            }

            if (Data.Schedule.Trigger.Any())
            {
                Crons = new List<CronDTO>(Data.Schedule.Trigger.Select(x => new CronDTO(x.Cron))).OrderBy(x => x.Name).ToList();
            }
        }

        public async Task<IActionResult> OnPostRestartAsync()
        {
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            var currentScheduler = await schedulerFactory.GetScheduler("default");
            await currentScheduler.Shutdown(false);
            var configuration = AutofacConfig.Container.Resolve<IConfiguration>();
            var properties = new QuartzOption(configuration).ToProperties();

            schedulerFactory = new StdSchedulerFactory(properties);
            var scheduler = await schedulerFactory.GetScheduler();
            await scheduler.Start();

            return RedirectToPage("./Index", new { msg = "Scheduler restarted" });
        }

        public IActionResult OnPostKillAll()
        {
            ActivityBag activities = new();
            foreach (var item in activities)
            {
                item?.Kill();
            }
            return RedirectToPage("./Index", new { msg = "Jobs stoped" });
        }
    }
}
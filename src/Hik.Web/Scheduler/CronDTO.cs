using CronExpressionDescriptor;
using Job.Extensions;
using System.Linq;

namespace Hik.Web.Scheduler
{
    public class CronDTO
    {
        internal static readonly Options CronFormatOptions = new() { DayOfWeekStartIndexZero = false, Use24HourTimeFormat = true };

        public CronDTO() { }
        public CronDTO(Cron cron)
        {
            Name = cron.Name;
            Description = cron.Description;
            Group = cron.Group;
            Cronexpression = cron.Cronexpression;
            ConfigPath = cron.Jobdatamap.Entry.FirstOrDefault(x => x.Key == CronTriggerImplExtensions.Config)?.Value;
            Job = cron.Jobdatamap.Entry.FirstOrDefault(x => x.Key == CronTriggerImplExtensions.Job)?.Value;
            RunAsTask = bool.Parse(cron.Jobdatamap.Entry.FirstOrDefault(x => x.Key == CronTriggerImplExtensions.RunAsTask)?.Value);
        }

        public string Name { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public string Job { get; set; }
        public string ConfigPath { get; set; }
        public bool RunAsTask { get; set; }
        public string Cronexpression { get; set; }
        public string CronDescription => string.IsNullOrEmpty(Cronexpression) ?
            string.Empty :
            ExpressionDescriptor.GetDescription(Cronexpression, CronFormatOptions);

        public Cron ToCron()
        {
            var configPath = GetJobDataMap("ConfigPath", ConfigPath);
            var job = GetJobDataMap("Job", Job);
            var runAsTask = GetJobDataMap("RunAsTask", RunAsTask.ToString());
            var modified = new Cron()
            {
                Cronexpression = Cronexpression,
                Description = Description,
                Group = Group,
                Name = Name,
                Jobname = "Launcher",
                Misfireinstruction = "DoNothing",
                Jobdatamap = new JobDataMap { Entry = new System.Collections.Generic.List<Entry>(new[] { configPath, runAsTask, job }.AsEnumerable()) }
            };

            return modified;
        }

        private static Entry GetJobDataMap(string key, string value)
        {
            return new Entry { Key = key, Value = value };
        }
    }
}

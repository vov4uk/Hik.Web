using CronExpressionDescriptor;
using Hik.Quartz.Contracts.Xml;
using Hik.Quartz.Extensions;
using Quartz.Impl.Triggers;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Hik.Quartz.Contracts
{
    public class CronDTO
    {
        internal static readonly CronExpressionDescriptor.Options CronFormatOptions = new CronExpressionDescriptor.Options() { DayOfWeekStartIndexZero = false, Use24HourTimeFormat = true };

        public CronDTO() { }
        public CronDTO(Cron cron)
        {
            Name = cron.Name;
            Description = cron.Description;
            Group = cron.Group;
            Expression = cron.Cronexpression;
            ConfigPath = cron.Jobdatamap.Entry.FirstOrDefault(x => x.Key == CronTriggerImplExtensions.Config)?.Value;
            ClassName = cron.Jobdatamap.Entry.FirstOrDefault(x => x.Key == CronTriggerImplExtensions.Job)?.Value;
            RunAsTask = bool.Parse(cron.Jobdatamap.Entry.FirstOrDefault(x => x.Key == CronTriggerImplExtensions.RunAsTask)?.Value);
        }

        public CronDTO(string configPath, string expression, DateTime triggerStarted, DateTime? next)
        {
            ConfigPath = configPath;
            Expression = expression;
            TriggerStarted = triggerStarted;
            NextRun = next;
        }

        public CronDTO(CronTriggerImpl cron)
        {
            ConfigPath = cron.GetConfig();
            Expression = cron.CronExpressionString;
            Description = cron.Description;
            TriggerStarted = cron.StartTimeUtc.DateTime.ToLocalTime();
            NextRun = cron.GetNextFireTimeUtc().Value.DateTime.ToLocalTime();
            Name = cron.Name;
            Group = cron.Group;
            ClassName = cron.GetJobClass();
            RunAsTask = cron.GetRunAsTask();
        }

        [Display(Name = "Config")]
        public string ConfigPath { get; set; }

        public string Description { get; set; }

        [Display(Name = "Cron")]
        public string Expression { get; set; }

        public string ExpressionString => string.IsNullOrEmpty(Expression) ?
            string.Empty :
            ExpressionDescriptor.GetDescription(Expression, CronFormatOptions);

        public string Group { get; set; }

        [Display(Name = "Class name")]
        public string ClassName { get; set; }

        public string Name { get; set; }
        [Display(Name = "Next"), DisplayFormat(DataFormatString = Consts.DisplayTimeFormat), DataType(DataType.DateTime)]
        public DateTime? NextRun { get; set; }

        [Display(Name = "Run as task")]
        public bool RunAsTask { get; set; }

        [Display(Name = "Trigger"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime TriggerStarted { get; set; }

        public Cron ToCron()
        {
            var configPath = GetJobDataMap(CronTriggerImplExtensions.Config, ConfigPath);
            var job = GetJobDataMap(CronTriggerImplExtensions.Job, ClassName);
            var runAsTask = GetJobDataMap(CronTriggerImplExtensions.RunAsTask, RunAsTask.ToString());
            var modified = new Cron()
            {
                Cronexpression = Expression,
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

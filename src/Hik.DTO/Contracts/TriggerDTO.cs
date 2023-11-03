using CronExpressionDescriptor;
using Hik.Quartz.Contracts.Xml;
using Newtonsoft.Json;
using Quartz;
using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class TriggerDto
    {
        private static readonly Options CronFormatOptions = new Options()
        {
            DayOfWeekStartIndexZero = false,
            Use24HourTimeFormat = true,
            Locale = "uk"
        };

        public int Id { get; set; } = 0;

        public string Group { get; set; } = "JobHost";

        public string Name { get; set; }

        [JsonIgnore]
        [Display(Name = "Class name")]
        public string ClassName { get; set; }

        public string Description { get; set; }

        [Display(Name = "Cron")]
        public string CronExpression { get; set; }

        [Display(Name = "Config")]
        public string Config { get; set; }

        [JsonIgnore]
        public string ExpressionString => string.IsNullOrEmpty(CronExpression) ?
            string.Empty :
            ExpressionDescriptor.GetDescription(CronExpression, CronFormatOptions);

        [Display(Name = "Synced"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastSync { get; set; }

        [JsonIgnore]
        [Display(Name = "Next"), DisplayFormat(DataFormatString = Consts.DisplayTimeFormat), DataType(DataType.DateTime)]
        public DateTime? NextRun
        { get
            {
                return new CronExpression(this.CronExpression).GetTimeAfter(DateTimeOffset.UtcNow).Value.DateTime.ToLocalTime();
            }
        }

        [Display(Name = "Enabled")]
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "Sent email on error")]
        public bool SentEmailOnError { get; set; } = true;

        [Display(Name = "Show in search")]
        public bool ShowInSearch { get; set; }

        public int? ProcessId { get; set; }

        public HikJobDto LastJob { get; set; }

        public Cron ToCron()
        {
            return new Cron()
            {
                Cronexpression = CronExpression,
                Description = Description,
                Group = Group,
                Name = Name,
                Jobname = "Launcher",
                Misfireinstruction = "DoNothing"
            };
        }

        public override string ToString()
        {
            return $"{Group}.{Name}";
        }
    }
}

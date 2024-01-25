using CronExpressionDescriptor;
using FluentValidation;
using Hik.DTO.Config;
using Hik.Quartz.Contracts.Xml;
using Newtonsoft.Json;
using Quartz;
using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class TriggerDto
    {
        BaseConfig config = null;

        private static readonly Options CronFormatOptions = new Options()
        {
            DayOfWeekStartIndexZero = false,
            Use24HourTimeFormat = true,
            Locale = "uk"
        };

        public int Id { get; set; } = 0;

        public string Group { get; set; } = "JobHost";

        public string Name { get; set; }

        [Display(Name = "Class name")]
        public string ClassName { get; set; }

        public string Description { get; set; }

        [Display(Name = "Cron")]
        public string CronExpression { get; set; } = "";

        [Display(Name = "Config")]
        public string Config { get; set; }

        [JsonIgnore]
        public BaseConfig ConfigDto
        {
            get
            {
                if (!string.IsNullOrEmpty(Config) && config == null)
                {
                    switch (ClassName)
                    {
                        case "FilesCollectorJob": config = JsonConvert.DeserializeObject<FilesCollectorConfig>(Config); break;
                        case "ImagesCollectorJob": config = JsonConvert.DeserializeObject<ImagesCollectorConfig>(Config); break;
                        case "GarbageCollectorJob": config = JsonConvert.DeserializeObject<GarbageCollectorConfig>(Config); break;
                        case "PhotoDownloaderJob": config = JsonConvert.DeserializeObject<CameraConfig>(Config); break;
                        case "VideoDownloaderJob": config = JsonConvert.DeserializeObject<CameraConfig>(Config); break;
                        default:
                            config = null;  break;
                    }
                }
                return config;
            }
            set
            {
               config = value;
            }
        }

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
                return string.IsNullOrEmpty(this.CronExpression) ? default : new CronExpression(this.CronExpression).GetTimeAfter(DateTimeOffset.UtcNow).Value.DateTime.ToLocalTime();
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

    public class TriggerDtoValidator : AbstractValidator<TriggerDto>
    {
        public TriggerDtoValidator()
        {
            RuleFor(x => x.Group).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.ClassName).NotEmpty();
            RuleFor(x => x.CronExpression).NotEmpty();
            RuleFor(x => x.Description).NotEmpty();
        }
    }
}

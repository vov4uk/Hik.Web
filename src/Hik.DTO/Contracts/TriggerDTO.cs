using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class TriggerDTO
    {
        public const string DisplayDateTimeFormat = "{0:yyyy'-'MM'-'dd' 'HH':'mm':'ss}";
        public const string DisplayTimeFormat = "{0:HH':'mm':'ss}";

        public string Group { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        [Display(Name = "Config")]
        public string ConfigPath { get; set; }

        [Display(Name = "Cron")]
        public string CronString { get; set; }
        public string CronSummary { get; set; }

        [Display(Name = "Trigger"), DisplayFormat(DataFormatString = DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime TriggerStarted { get; set; }

        [Display(Name = "Activity"), DisplayFormat(DataFormatString = DisplayTimeFormat), DataType(DataType.DateTime)]
        public DateTime? ActivityStarted { get; set; }    
        
        [Display(Name = "Next"), DisplayFormat(DataFormatString = DisplayTimeFormat), DataType(DataType.DateTime)]
        public DateTime? Next { get; set; }

        public Guid? ActivityId { get; set; }

        public int JobTriggerId { get; set; }

        public int? JobId { get; set; }

        [Display(Name = "Sync"), DisplayFormat(DataFormatString = DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastSync { get; set; }

        [Display(Name = "From"), DisplayFormat(DataFormatString = DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastJobPeriodStart { get; set; }

        [Display(Name = "To"), DisplayFormat(DataFormatString = DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastJobPeriodEnd { get; set; }    
        
        [Display(Name = "Started"), DisplayFormat(DataFormatString = DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastJobStarted { get; set; }

        [Display(Name = "Finished"), DisplayFormat(DataFormatString = DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastJobFinished { get; set; }

        public int? LastJobFilesCount { get; set; }

        public bool Success { get; set; }

        public int? ProcessId { get; set; }
    }
}

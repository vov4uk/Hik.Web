using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class TriggerDTO
    {
        public string Group { get; set; }

        public string Name { get; set; }

        [Display(Name = "Config")]
        public string ConfigPath { get; set; }

        [Display(Name = "Cron")]
        public string CronString { get; set; }

        public string CronSummary { get; set; }

        [Display(Name = "Trigger"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime TriggerStarted { get; set; }

        [Display(Name = "Next"), DisplayFormat(DataFormatString = Consts.DisplayTimeFormat), DataType(DataType.DateTime)]
        public DateTime? Next { get; set; }

        public string ActivityId { get; set; }

        public int JobTriggerId { get; set; }

        public int? JobId { get; set; }

        [Display(Name = "Synced"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastSync { get; set; }

        [Display(Name = "From"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastJobPeriodStart { get; set; }

        [Display(Name = "To"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastJobPeriodEnd { get; set; }

        [Display(Name = "Started"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastJobStarted { get; set; }

        [Display(Name = "Finished"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastJobFinished { get; set; }

        [Display(Name = "Files")]
        public int? LastJobFilesCount { get; set; }

        public bool Success { get; set; }

        public int? ProcessId { get; set; }

        public override string ToString()
        {
            return $"{Group}.{Name}";
        }
    }
}

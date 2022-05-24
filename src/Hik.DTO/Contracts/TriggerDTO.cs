using Hik.Quartz.Contracts;
using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class TriggerDto
    {
        public string Group { get; set; }

        public string Name { get; set; }

        public int JobTriggerId { get; set; }

        [Display(Name = "Synced"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastSync { get; set; }

        public int? ProcessId { get; set; }

        public HikJobDto LastJob { get; set; }

        public CronDto Cron { get; set; }

        public override string ToString()
        {
            return $"{Group}.{Name}";
        }
    }
}

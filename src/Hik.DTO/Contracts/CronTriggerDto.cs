using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class CronTriggerDto
    {
        public CronTriggerDto() { }

        public CronTriggerDto(string configPath, string expression, string expressionString, DateTime triggerStarted, DateTime? next)
        {
            ConfigPath = configPath;
            Expression = expression;
            ExpressionString = expressionString;
            TriggerStarted = triggerStarted;
            Next = next;
        }

        [Display(Name = "Config")]
        public string ConfigPath { get; set; }

        [Display(Name = "Cron")]
        public string Expression { get; set; }

        public string ExpressionString { get; set; }

        [Display(Name = "Trigger"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime TriggerStarted { get; set; }

        [Display(Name = "Next"), DisplayFormat(DataFormatString = Consts.DisplayTimeFormat), DataType(DataType.DateTime)]
        public DateTime? Next { get; set; }

    }
}

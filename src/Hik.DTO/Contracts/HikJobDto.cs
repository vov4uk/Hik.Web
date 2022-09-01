using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class HikJobDto
    {
        public int Id { get; set; }

        public string JobTrigger { get; set; }

        public int JobTriggerId { get; set; }

        public bool Success { get; set; } = true;

        [Display(Name = "From"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? PeriodStart { get; set; }

        [Display(Name = "To"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? PeriodEnd { get; set; }

        [DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime Started { get; set; }

        [DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? Finished { get; set; }

        [Display(Name = "Files")]
        public int FilesCount { get; set; }

        public ExceptionLogDto Error { get; set; }
    }
}

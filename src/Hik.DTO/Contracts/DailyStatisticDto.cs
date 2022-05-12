using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class DailyStatisticDto
    {
        [Display(Name = "Day"), DisplayFormat(DataFormatString = Consts.DisplayDayFormat), DataType(DataType.DateTime)]
        public DateTime Period { get; set; }

        [Display(Name = "Count")]
        public int FilesCount { get; set; }

        [Display(Name = "Size")]
        public long FilesSize { get; set; }

        [Display(Name = "Duration")]
        public int? TotalDuration { get; set; }
    }
}

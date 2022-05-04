using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Metadata;

namespace Hik.DataAccess.Data
{
    [ExcludeFromCodeCoverage, Table(Tables.DailyStatistics)]
    public class DailyStatistic
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("JobTrigger")]
        public int JobTriggerId { get; set; }

        [Display(Name = "Day"), DisplayFormat(DataFormatString = Consts.DisplayDayFormat), DataType(DataType.DateTime)]
        public DateTime Period { get; set; }

        [Display(Name = "Count")]
        public int FilesCount { get; set; }

        [Display(Name = "Size")]
        public long FilesSize { get; set; }
        
        [Display(Name = "Duration")]
        public int? TotalDuration { get; set; }

        public virtual JobTrigger JobTrigger { get; set; }
    }
}

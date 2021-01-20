using Hik.DataAccess.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hik.DataAccess.Data
{
    [Table("Job")]
    public class HikJob
    {
        [Key]
        public int Id { get; set; }

        public string JobType { get; set; }

        [Display(Name = "Success")]
        public bool Success { get; set; }

        [Display(Name = "From"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public  DateTime? PeriodStart { get; set; }

        [Display(Name = "To"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? PeriodEnd { get; set; }

        [DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime Started { get; set; }

        [DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? Finished { get; set; }

        [Display(Name = "Files")]
        public int FilesCount { get; set; }

        public ExceptionLog ExceptionLog { get; set; }

        public List<MediaFile> Files { get; set; } = new List<MediaFile>();
    }
}

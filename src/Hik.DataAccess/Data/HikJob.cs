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

        [Display(Name = "Photo")]
        public int PhotosCount { get; set; }

        [Display(Name = "Video")]
        public int VideosCount { get; set; }

        public List<Video> Videos { get; set; } = new List<Video>();

        public ExceptionLog ExceptionLog { get; set; }

        public List<Photo> Photos { get; set; } = new List<Photo>();

        public List<File> Files { get; set; } = new List<File>();

        public List<DeletedFile> DeletedFiles { get; set; } = new List<DeletedFile>();
    }
}

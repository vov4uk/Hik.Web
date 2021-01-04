using Hik.DataAccess.Metadata;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hik.DataAccess.Data
{
    [Table("Photo")]
    public class Photo : IAuditable
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Camera")]
        public int CameraId { get; set; }

        [ForeignKey("Job")]
        public int JobId { get; set; }

        public string Name { get; set; }

        [Display(Name = "Taken"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime DateTaken { get; set; }

        public long Size { get; set; }

        [Display(Name = "Started"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime DownloadStartTime { get; set; }

        [Display(Name = "Finished"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime DownloadStopTime { get; set; }

        public virtual Camera Camera { get; set; }

        public virtual HikJob Job { get; set; }
    }
}

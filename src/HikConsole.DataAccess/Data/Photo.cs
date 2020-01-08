using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikConsole.DataAccess.Data
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

        [Display(Name = "Taken")]
        public DateTime DateTaken { get; set; }

        public long Size { get; set; }

        public long? LocalSize { get; set; }

        [Display(Name = "Started")]
        public DateTime DownloadStartTime { get; set; }

        [Display(Name = "Finished")]
        public DateTime DownloadStopTime { get; set; }

        public virtual Camera Camera { get; set; }

        public virtual HikJob Job { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace HikConsole.DataAccess.Data
{
    [Table("Video")]
    public class Video : IAuditable
    {
        [Key]
        public  int Id { get; set; }

        [ForeignKey("Camera")]
        public int CameraId { get; set; }

        [ForeignKey("Job")]
        public int JobId { get; set; }

        public string Name { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime StopTime { get; set; } 

        public DateTime DownloadStartTime { get; set; }

        public DateTime DownloadStopTime { get; set; }

        public long Size { get; set; }

        public long LocalSize { get; set; }

        public virtual Camera Camera { get; set; }

        public virtual Job Job { get; set; }
    }
}

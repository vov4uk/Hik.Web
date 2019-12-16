using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikConsole.DataAccess.Data
{
    [Table("Photo")]
    public class Photo
    {
        [Key]
        public int Id { get; set; }

        public int CameraId { get; set; }

        public int JobId { get; set; }

        public string Name { get; set; }

        public DateTime DateTaken { get; set; }

        public long Size { get; set; }

        public long LocalSize { get; set; }

        public DateTime DownloadStartTime { get; set; }

        public DateTime DownloadStopTime { get; set; }
    }
}

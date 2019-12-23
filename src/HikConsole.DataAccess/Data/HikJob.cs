using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikConsole.DataAccess.Data
{
    [Table("Job")]
    public class HikJob
    {
        [Key]
        public int Id { get; set; }

        public string JobType { get; set; }

        public int FailsCount { get; set; }

        public  DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        public DateTime Started { get; set; }

        public DateTime Finished { get; set; }

        public int PhotosCount { get; set; }

        public int VideosCount { get; set; }

        public List<Video> Videos { get; set; } = new List<Video>();

        public List<Photo> Photos { get; set; } = new List<Photo>();

        public List<HardDriveStatus> HardDriveStatuses { get; set; } = new List<HardDriveStatus>();

        public List<DeletedFile> DeletedFiles { get; set; } = new List<DeletedFile>();
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hik.DataAccess.Metadata;

namespace Hik.DataAccess.Data
{
    [Table(Tables.Camera)]
    public class Camera
    {
        [Key] 
        public int Id { get; set; }

        public string Alias { get; set; }

        public string DestinationFolder { get; set; }

        public string IpAddress { get; set; }

        public int PortNumber { get; set; }

        public string UserName { get; set; }

        public List<Video> Videos { get; set; } = new List<Video>();

        public List<Photo> Photos { get; set; } = new List<Photo>();

        public List<File> Files { get; set; } = new List<File>();

        public List<DeletedFile> DeletedFiles { get; set; } = new List<DeletedFile>();

        public List<DailyStatistic> DailyStatistics { get; set; } = new List<DailyStatistic>();

        [Display(Name = "Video Synced"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastVideoSync { get; set; }

        [Display(Name = "Photo Synced"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastPhotoSync { get; set; }
    }
}
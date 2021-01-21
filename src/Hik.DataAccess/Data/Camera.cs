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

        public List<MediaFile> Files { get; set; } = new List<MediaFile>();

        public List<DailyStatistic> DailyStatistics { get; set; } = new List<DailyStatistic>();

        [Display(Name = "Synced"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastSync { get; set; }
    }
}
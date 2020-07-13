using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HikConsole.DataAccess.Metadata;

namespace HikConsole.DataAccess.Data
{
    [Table("Camera")]
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

        public List<HardDriveStatus> HardDriveStatuses { get; set; } = new List<HardDriveStatus>();

        public List<DeletedFile> DeletedFiles { get; set; } = new List<DeletedFile>();

        [DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastSync { get; set; }
    }
}
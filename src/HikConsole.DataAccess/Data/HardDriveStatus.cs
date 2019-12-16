using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikConsole.DataAccess.Data
{
    [Table("HardDriveStatus")]
    public class HardDriveStatus
    {
        [Key]
        public int Id { get; set; }
        public int CameraId { get; set; }

        public int JobId { get; set; }

        public uint Capacity { get; set; }
        public uint FreeSpace { get; set; }
        public uint HdStatus { get; set; }
        public byte HDAttr { get; set; }
        public byte HDType { get; set; }
        public byte Recycling { get; set; }
        public uint PictureCapacity { get; set; }
        public uint FreePictureSpace { get; set; }
    }
}

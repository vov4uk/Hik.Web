using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikConsole.DataAccess.Data
{
    [Table("HardDriveStatus")]
    public class HardDriveStatus : IAuditable
    {
        [Key] 
        public int Id { get; set; }

        [ForeignKey("Camera")]
        public int CameraId { get; set; }

        [ForeignKey("Job")]
        public int JobId { get; set; }

        public uint Capacity { get; set; }

        [Display(Name = "Free Space")]
        public uint FreeSpace { get; set; }

        [Display(Name = "Status")]
        public uint HdStatus { get; set; }

        public byte HDAttr { get; set; }

        [Display(Name = "Type")]
        public byte HDType { get; set; }

        public byte Recycling { get; set; }

        public uint PictureCapacity { get; set; }

        public uint FreePictureSpace { get; set; }

        public virtual Camera Camera { get; set; }

        public virtual HikJob Job { get; set; }
    }
}
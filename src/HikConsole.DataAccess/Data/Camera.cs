using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HikConsole.DataAccess.Data
{
    [Table("Camera")]
    public class Camera
    {
        [Key] public int Id { get; set; }

        public string Alias { get; set; }

        public string DestinationFolder { get; set; }

        public string IpAddress { get; set; }

        public int PortNumber { get; set; }

        public string UserName { get; set; }
    }
}
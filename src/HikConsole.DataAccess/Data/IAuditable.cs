using System.ComponentModel.DataAnnotations.Schema;

namespace HikConsole.DataAccess.Data
{
    public interface IAuditable
    {
        [ForeignKey("Camera")]
        int CameraId { get; set; }

        [ForeignKey("Job")]
        int JobId { get; set; }

        Camera Camera { get; set; }

        HikJob Job { get; set; }
    }
}

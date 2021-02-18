using System.ComponentModel.DataAnnotations.Schema;

namespace Hik.DataAccess.Data
{
    public interface IAuditable
    {
        [ForeignKey("Job")]
        int JobId { get; set; }

        HikJob Job { get; set; }
    }
}

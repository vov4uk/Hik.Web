using Hik.DataAccess.Data;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hik.DataAccess.Abstractions
{
    public interface IAuditable
    {
        [ForeignKey("Job")]
        int JobId { get; set; }

        HikJob Job { get; set; }
    }
}

using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Metadata;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hik.DataAccess.Data
{
    [Table(Tables.DeleteHistory)]
    public sealed class DeleteHistory : IAuditable, IHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int MediaFileId { get; set; }

        public int JobId { get; set; }

        public HikJob Job { get; set; }

        public MediaFile MediaFile { get; set; }
    }
}

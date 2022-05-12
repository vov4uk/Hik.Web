using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Metadata;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Hik.DataAccess.Data
{
    [ExcludeFromCodeCoverage, Table(Tables.DownloadDuration)]
    public class DownloadDuration: IEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey(nameof(MediaFile))]
        public int MediaFileId { get; set; }

        public DateTime? Started { get; set; }

        public int? Duration { get; set; }

        public virtual MediaFile MediaFile { get; set; }
    }
}

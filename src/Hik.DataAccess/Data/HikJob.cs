using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Hik.DataAccess.Data
{
    [ExcludeFromCodeCoverage, Table(Tables.Job)]
    public class HikJob : IEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("FK_Job_JobTrigger_JobTriggerId")]
        public int JobTriggerId { get; set; }

        [NotMapped]
        public DateTime? LatestFileEndDate { get; set; }

        public bool Success { get; set; } = true;

        public  DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        public DateTime Started { get; set; }

        public DateTime? Finished { get; set; }

        public int FilesCount { get; set; }

        [NotMapped]
        public virtual JobTrigger JobTrigger { get; set; }

        public virtual ExceptionLog ExceptionLog { get; set; }

        public virtual List<MediaFile> DownloadedFiles { get; set; } = new List<MediaFile>();
    }
}

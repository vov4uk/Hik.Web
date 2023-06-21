using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Metadata;

namespace Hik.DataAccess.Data
{
    [ExcludeFromCodeCoverage, Table(Tables.JobTrigger)]
    public class JobTrigger : IEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string TriggerKey { get; set; }

        public string Group { get; set; }

        public bool ShowInSearch { get; set; }

        public virtual List<DailyStatistic> DailyStatistics { get; set; } = new List<DailyStatistic>();

        public virtual List<HikJob> Jobs { get; set; } = new List<HikJob>();

        public DateTime? LastSync { get; set; }

        public int? LastExecutedJobId { get; set; }

        [ForeignKey("LastExecutedJobId")]
        public virtual HikJob LastExecutedJob { get; set; }

        public int? LastSuccessJobId { get; set; }

        [ForeignKey("LastSuccessJobId")]
        public virtual HikJob LastSuccessJob { get; set; }

        public virtual List<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();

        public override string ToString()
        {
            return $"{Group}.{TriggerKey}";
        }
    }
}

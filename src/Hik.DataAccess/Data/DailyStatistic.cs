using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Metadata;

namespace Hik.DataAccess.Data
{
    [ExcludeFromCodeCoverage, Table(Tables.DailyStatistics)]
    public class DailyStatistic : IEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("JobTrigger")]
        public int JobTriggerId { get; set; }

        public DateTime Period { get; set; }

        public int FilesCount { get; set; }

        public long FilesSize { get; set; }

        public int? TotalDuration { get; set; }

        public virtual JobTrigger JobTrigger { get; set; }
    }
}

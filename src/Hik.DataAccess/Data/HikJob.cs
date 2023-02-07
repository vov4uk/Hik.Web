using AutoMapper.Configuration.Annotations;
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

        [ForeignKey("JobTrigger")]
        public int JobTriggerId { get; set; }

        [NotMapped]
        public DateTime? LatestFileEndDate { get; set; }

        public bool Success { get; set; } = true;

        public  DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        public DateTime Started { get; set; }

        public DateTime? Finished { get; set; }

        public int FilesCount { get; set; }

        public JobTrigger JobTrigger { get; set; }

        public ExceptionLog ExceptionLog { get; set; }

        public List<DownloadHistory> DownloadedFiles { get; set; } = new List<DownloadHistory>();
    }
}

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

        public List<DailyStatistic> DailyStatistics { get; set; } = new List<DailyStatistic>();

        public List<HikJob> Jobs { get; set; } = new List<HikJob>();

        [Display(Name = "Synced"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastSync { get; set; }

        public virtual List<MediaFile> MediaFiles { get; set; } = new List<MediaFile>();

        public override string ToString()
        {
            return $"{Group}.{TriggerKey}";
        }
    }
}

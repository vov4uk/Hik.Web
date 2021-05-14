using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hik.DataAccess.Metadata;

namespace Hik.DataAccess.Data
{
    [Table(Tables.JobTrigger)]
    public sealed class JobTrigger
    {
        [Key]
        public int Id { get; set; }

        public string TriggerKey { get; set; }

        public string Group { get; set; }

        public List<DailyStatistic> DailyStatistics { get; } = new List<DailyStatistic>();

        public List<HikJob> Jobs { get; set; } = new List<HikJob>();

        [Display(Name = "Synced"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? LastSync { get; set; }

        public List<MediaFile> MediaFiles { get; } = new List<MediaFile>();

        public override string ToString()
        {
            return $"{Group}.{TriggerKey}";
        }
    }
}

using Hik.DataAccess.Metadata;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hik.DataAccess.Data
{
    [Table(Tables.MediaFile)]
    public sealed class MediaFile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("JobTrigger")]
        public int JobTriggerId { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        [Display(Name = "Date"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        public int? Duration { get; set; }

        public long Size { get; set; }


        public JobTrigger JobTrigger { get; set; }

        public DownloadDuration DownloadDuration { get; set; }

        public DownloadHistory DownloadHistory { get; set; }

        public DeleteHistory DeleteHistory { get; set; }
    }
}

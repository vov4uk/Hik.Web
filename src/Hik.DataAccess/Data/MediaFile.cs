using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Metadata;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace Hik.DataAccess.Data
{
    [ExcludeFromCodeCoverage, Table(Tables.MediaFile)]
    public class MediaFile : IEntity, IAuditable
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("FK_MediaFile_JobTrigger_JobTriggerId")]
        public int JobTriggerId { get; set; }

        [ForeignKey("FK_MediaFile_Job_JobId")]
        public int JobId { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string Objects { get; set; }

        public DateTime Date { get; set; }

        public int? Duration { get; set; }

        public DateTime DownloadStarted { get; set; }

        public int? DownloadDuration { get; set; }

        public long Size { get; set; }

        public JobTrigger JobTrigger { get; set; }

        public HikJob Job { get; set; }

        public string GetPath()
        {
            if (!string.IsNullOrEmpty(Path) && !System.IO.Path.HasExtension(this.Path))
            {
                return System.IO.Path.Combine(this.Path, this.Name);
            }
            return this.Path ?? string.Empty;
        }
    }
}

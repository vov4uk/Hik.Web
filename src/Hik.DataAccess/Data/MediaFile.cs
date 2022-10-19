using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Metadata;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hik.DataAccess.Data
{
    [ExcludeFromCodeCoverage, Table(Tables.MediaFile)]
    public class MediaFile : IEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("JobTrigger")]
        public int JobTriggerId { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string Objects { get; set; }

        public DateTime Date { get; set; }

        public int? Duration { get; set; }

        public long Size { get; set; }

        public JobTrigger JobTrigger { get; set; }

        public virtual DownloadDuration DownloadDuration { get; set; }

        public virtual DownloadHistory DownloadHistory { get; set; }

        public string GetPath()
        {
            if (Debugger.IsAttached)
            {
                return this.Path.Replace("E:\\Cloud\\", "W:\\");
            }

            if (!System.IO.Path.HasExtension(this.Path))
            {
                return System.IO.Path.Combine(this.Path, this.Name);
            }
            return this.Path;
        }
    }
}

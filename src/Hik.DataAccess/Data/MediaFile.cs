using Hik.DataAccess.Metadata;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.IO;

namespace Hik.DataAccess.Data
{
    [Table(Tables.MediaFile)]
    public class MediaFile
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

        public virtual DownloadDuration DownloadDuration { get; set; }

        public virtual DownloadHistory DownloadHistory { get; set; }
    }

    public static class MediaFileExtensions
    {
        public static string GetPath(this MediaFile file)
        {
            if (Debugger.IsAttached)
            {
                return file.Path.Replace("E:\\Cloud\\", "W:\\");
            }

            if (!Path.HasExtension(file.Path))
            {
                return Path.Combine(file.Path, file.Name);
            }
            return file.Path;
        }
    }
}

using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Metadata;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hik.DataAccess.Data
{
    [Table(Tables.DownloadDuration)]
    public class DownloadDuration: IHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int MediaFileId { get; set; }

        [Display(Name = "Started"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? Started { get; set; }

        [Display(Name = "Downloaded")]
        public int? Duration { get; set; }

        public virtual MediaFile MediaFile { get; set; }
    }
}

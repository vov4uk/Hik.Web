using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Hik.DataAccess.Metadata;

namespace Hik.DataAccess.Data
{
    [Table("DailyStatistic")]
    public class DailyStatistic
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Camera")]
        public int CameraId { get; set; }

        [Display(Name = "Day"), DisplayFormat(DataFormatString = Consts.DisplayDayFormat), DataType(DataType.DateTime)]
        public DateTime Period { get; set; }

        [Display(Name = "Count")]
        public int FilesCount { get; set; }

        [Display(Name = "Size")]
        public long FilesSize { get; set; }

        public virtual Camera Camera { get; set; }
    }
}

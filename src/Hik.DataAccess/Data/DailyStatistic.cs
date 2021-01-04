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

        [Display(Name = "Photos Count")]
        public int PhotosCount { get; set; }

        [Display(Name = "Videos Count")]
        public int VideosCount { get; set; }

        [Display(Name = "Photos Size")]
        public long PhotosSize { get; set; }

        [Display(Name = "Video Size")]
        public long VideosSize { get; set; }

        public virtual Camera Camera { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace Hik.DTO.Contracts
{
    public class MediaFileDTO
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public long Size { get; set; }

        [Display(Name = "Started"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime? DownloadStarted { get; set; }

        [Display(Name = "Downloaded")]
        public int? DownloadDuration { get; set; }

        [Display(Name = "Date"), DisplayFormat(DataFormatString = Consts.DisplayDateTimeFormat), DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        public int? Duration { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}

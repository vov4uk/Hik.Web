using System;

namespace HikConsole.DTO.Contracts
{
    public class PhotoDTO
    {
        public string Name { get; set; }

        public DateTime DateTaken { get; set; }

        public long Size { get; set; }

        public DateTime DownloadStartTime { get; set; }

        public DateTime DownloadStopTime { get; set; }
    }
}

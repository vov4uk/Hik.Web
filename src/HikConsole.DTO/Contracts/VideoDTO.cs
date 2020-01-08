using System;

namespace HikConsole.DTO.Contracts
{
    public class VideoDTO
    {
        public string Name { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime StopTime { get; set; }

        public DateTime DownloadStartTime { get; set; }

        public DateTime DownloadStopTime { get; set; }

        public long Size { get; set; }

        public long LocalSize { get; set; }
    }
}

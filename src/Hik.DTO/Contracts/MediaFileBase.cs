using System;

namespace Hik.DTO.Contracts
{
    public class MediaFileBase
    {
        public string Name { get; set; }

        public long Size { get; set; }

        public DateTime DownloadStartTime { get; set; }

        public DateTime DownloadStopTime { get; set; }
    }
}

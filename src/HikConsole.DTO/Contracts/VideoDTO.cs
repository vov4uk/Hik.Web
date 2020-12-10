using System;

namespace HikConsole.DTO.Contracts
{
    public class VideoDTO : MediaFileBase
    {
        public DateTime StartTime { get; set; }

        public DateTime StopTime { get; set; }
    }
}

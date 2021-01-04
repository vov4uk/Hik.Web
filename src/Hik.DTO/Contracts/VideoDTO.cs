using System;

namespace Hik.DTO.Contracts
{
    public class VideoDTO : MediaFileBase
    {
        public DateTime StartTime { get; set; }

        public DateTime StopTime { get; set; }
    }
}

using System;
using System.Collections.Generic;

namespace HikConsole.DTO
{
    public class JobResult
    {
        public JobResult()
        {
            this.CameraResults = new Dictionary<string, CameraResult>();
        }

        public DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        public Dictionary<string, CameraResult> CameraResults { get; }
    }
}

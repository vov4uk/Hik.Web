using System;
using System.Collections.Generic;

namespace Hik.DTO
{
    public class JobResult
    {
        public JobResult()
        {
            CameraResults = new Dictionary<string, CameraResult>();
        }

        public DateTime? PeriodStart { get; set; }

        public DateTime? PeriodEnd { get; set; }

        public Dictionary<string, CameraResult> CameraResults { get; }

        public void StoreCameraResult(string alias, CameraResult result)
        {
            CameraResults.Add(alias, result);
        }
    }
}

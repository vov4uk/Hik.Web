using System.Collections.Generic;
using HikConsole.DataAccess.Data;

namespace HikConsole.Scheduler
{
    public class JobResult
    {
        public JobResult(HikJob job)
        {
            this.Job = job;
            this.CameraResults = new Dictionary<string, CameraResult>();
        }

        public HikJob Job { get; }

        public Dictionary<string, CameraResult> CameraResults { get; }
    }
}

using System.Collections.Generic;
using HikConsole.DataAccess.Data;

namespace HikConsole.Scheduler
{
    public class JobResult
    {
        public JobResult(Job job)
        {
            this.Job = job;
            this.CameraResults = new Dictionary<string, CameraResult>();
        }

        public Job Job { get; }

        public Dictionary<string, CameraResult> CameraResults { get; }
    }
}

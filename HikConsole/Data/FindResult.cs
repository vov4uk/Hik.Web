using System;

namespace HikConsole.Data
{
    public class FindResult
    {
        public string FileName { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime StopTime { get; set; }

        public long FileSize { get; set; }
    }
}

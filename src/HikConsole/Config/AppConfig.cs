using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Newtonsoft.Json;

namespace HikConsole.Config
{
    [ExcludeFromCodeCoverage]
    public class AppConfig
    {
        public int ProcessingPeriodHours { get; set; }

        [JsonProperty("IntervalMinutes")]
        public int Interval { get; set; }

        [JsonProperty("JobTimeoutMinutes")]
        public int JobTimeout { get; set; } = 45;

        public string[] FilesToDelete { get; set; }

        public CameraConfig[] Cameras { get; set; }

        [JsonProperty("EmailSettings")]
        public EmailConfig EmailConfig { get; set; }

        public string ConnectionString { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"{"Interval",-24}: {this.Interval}m");
            sb.AppendLine($"{"JobTimeout",-24}: {this.JobTimeout}m");
            sb.AppendLine($"{"Period",-24}: {this.ProcessingPeriodHours}h");

            return sb.ToString();
        }

        public AppConfig Copy(CameraConfig camera)
        {
            return new AppConfig
            {
                ProcessingPeriodHours = this.ProcessingPeriodHours,
                JobTimeout = this.JobTimeout,
                Interval = this.Interval,
                FilesToDelete = this.FilesToDelete,
                Cameras = new[] { camera },
                EmailConfig = this.EmailConfig,
                ConnectionString = this.ConnectionString,
            };
        }
    }
}

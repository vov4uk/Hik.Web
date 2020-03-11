using System.Diagnostics.CodeAnalysis;
using System.Text;
using Newtonsoft.Json;

namespace HikConsole.Config
{
    [ExcludeFromCodeCoverage]
    public class AppConfig
    {
        public int ProcessingPeriodHours { get; set; }

        public string Mode { get; set; }

        [JsonProperty("IntervalMinutes")]
        public int Interval { get; set; }

        public string[] FilesToDelete { get; set; }

        public CameraConfig[] Cameras { get; set; }

        [JsonProperty("EmailSettings")]
        public EmailConfig EmailConfig { get; set; }

        public int? RetentionPeriodDays { get; set; } = 30;

        public string ConnectionString { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"{"Mode",-24}: {this.Mode}");
            sb.AppendLine($"{"Interval",-24}: {this.Interval.ToString()}m");
            sb.AppendLine($"{"Period",-24}: {this.ProcessingPeriodHours.ToString()}h");

            return sb.ToString();
        }
    }
}

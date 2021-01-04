using System.Diagnostics.CodeAnalysis;
using System.Text;
using Newtonsoft.Json;

namespace Hik.DTO.Config
{
    [ExcludeFromCodeCoverage]
    public class AppConfig
    {
        public int ProcessingPeriodHours { get; set; }

        [JsonProperty("JobTimeoutMinutes")]
        public int JobTimeout { get; set; } = 45;

        public string[] FilesToDelete { get; set; }

        public CameraConfig Camera { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"{"JobTimeout",-24}: {this.JobTimeout}m");
            sb.AppendLine($"{"Period",-24}: {this.ProcessingPeriodHours}h");

            return sb.ToString();
        }
    }
}

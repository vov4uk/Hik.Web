using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace HikConsole
{
    [ExcludeFromCodeCoverage]
    public class AppConfig
    {
        [JsonProperty("ProcessingPeriodHours")]
        public int ProcessingPeriodHours { get; set; }

        [JsonProperty("Mode")]
        public string Mode { get; set; }

        [JsonProperty("IntervalMinutes")]
        public int Interval { get; set; }

        [JsonProperty("Cameras")]
        public CameraConfig[] Cameras { get; set; }

        public override string ToString()
        {
            return $@"Mode        : {this.Mode}
Interval    : {this.Interval}m
Period      : {this.ProcessingPeriodHours}h";
        }
    }
}

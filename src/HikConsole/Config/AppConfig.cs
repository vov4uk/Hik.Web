using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace HikConsole.Config
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

        [JsonProperty("ShowProgress")]
        public bool ShowProgress { get; set; }

        [JsonProperty("Cameras")]
        public CameraConfig[] Cameras { get; set; }

        [JsonProperty("EmailSettings")]
        public EmailConfig EmailConfig { get; set; }

        public override string ToString()
        {
            return $@"Mode        : {this.Mode}
Interval    : {this.Interval.ToString()}m
Period      : {this.ProcessingPeriodHours.ToString()}h";
        }
    }
}

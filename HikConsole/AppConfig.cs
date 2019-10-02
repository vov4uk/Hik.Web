using Newtonsoft.Json;

namespace HikConsole
{
    public class AppConfig
    {
        [JsonProperty("IPAddress")]
        public string IpAddress { get; set; }

        [JsonProperty("PortNumber")]
        public int PortNumber { get; set; } = 8000;

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("Password")]
        public string Password { get; set; }

        [JsonProperty("ProcessingPeriodHours")]
        public int ProcessingPeriodHours { get; set; }

        [JsonProperty("DestinationFolder")]
        public string DestinationFolder { get; set; }

        [JsonProperty("Mode")]
        public string Mode { get; set; }

        [JsonProperty("IntervalMinutes")]
        public int Interval { get; set; }
    }
}

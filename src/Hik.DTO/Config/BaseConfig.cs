using Newtonsoft.Json;

namespace Hik.DTO.Config
{
    public class BaseConfig
    {
        [JsonProperty("JobTimeoutMinutes")]
        public int Timeout { get; set; } = 45;

        public string Alias { get; set; }

        public string DestinationFolder { get; set; }
    }
}

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace HikConsole.Config
{
    [ExcludeFromCodeCoverage]
    public class EmailConfig
    {
        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("Password")]
        public string Password { get; set; }

        [JsonProperty("Server")]
        public string Server { get; set; }

        [JsonProperty("Port")]
        public int Port { get; set; }

        [JsonProperty("Receiver")]
        public string Receiver { get; set; }
    }
}

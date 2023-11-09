using Newtonsoft.Json;
using System.Text;

namespace Hik.DTO.Config
{
    public abstract class BaseConfig
    {
        [JsonProperty("JobTimeoutMinutes")]
        public int Timeout { get; set; } = 29;

        public string DestinationFolder { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(this.GetRow(nameof(DestinationFolder), this.DestinationFolder));
            sb.AppendLine(this.GetRow(nameof(Timeout), this.Timeout.ToString()));

            return sb.ToString();
        }

        public virtual string ToHtmlTable()
        {
            var sb = new StringBuilder();
            sb.AppendLine(this.GetHtmlRow(nameof(DestinationFolder), this.DestinationFolder));
            sb.AppendLine(this.GetHtmlRow(nameof(Timeout), this.Timeout.ToString()));
            return sb.ToString();
        }

        protected string GetRow(string field, string value)
        {
            return $"{field,-24}: {value}";
        }

        protected string GetHtmlRow(string field, string value)
        {
            return $"<tr><td>{field}</td><td>{value}</td></tr>";
        }
    }
}

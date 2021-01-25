using Newtonsoft.Json;
using System.Text;

namespace Hik.DTO.Config
{
    public class BaseConfig
    {
        [JsonProperty("JobTimeoutMinutes")]
        public int Timeout { get; set; } = 45;

        public string Alias { get; set; }

        public string DestinationFolder { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(this.GetRow("Alias", this.Alias));
            sb.AppendLine(this.GetRow("Destination", this.DestinationFolder));
            sb.AppendLine(this.GetRow("Timeout", this.Timeout.ToString()));
  
            return sb.ToString();
        }

        public virtual string ToHtmlTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<table>");
            sb.AppendLine(this.GetHtmlRow("Alias", this.Alias));
            sb.AppendLine(this.GetHtmlRow("Destination", this.DestinationFolder));
            sb.AppendLine(this.GetHtmlRow("Timeout", this.Timeout.ToString()));
            sb.AppendLine("</table>");

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

using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Hik.DTO.Config
{
    public abstract class BaseConfig
    {
        [JsonProperty("JobTimeoutMinutes")]
        [Display(Name = "Job timeout")]
        public int Timeout { get; set; } = 29;

        [Display(Name = "Destination folder")]
        public string DestinationFolder { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetRow(nameof(DestinationFolder), this.DestinationFolder));
            sb.AppendLine(GetRow(nameof(Timeout), this.Timeout.ToString()));

            return sb.ToString();
        }

        public virtual string ToHtmlTable()
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetHtmlRow(nameof(DestinationFolder), this.DestinationFolder));
            sb.AppendLine(GetHtmlRow(nameof(Timeout), this.Timeout.ToString()));
            return sb.ToString();
        }

        protected static string GetRow(string field, string value)
        {
            return $"{field,-24}: {value}";
        }

        protected static string GetHtmlRow(string field, string value)
        {
            return $"<tr><td>{field}</td><td>{value}</td></tr>";
        }
    }
}

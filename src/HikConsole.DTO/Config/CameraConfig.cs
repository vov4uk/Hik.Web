using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HikConsole.DTO.Config
{
    [ExcludeFromCodeCoverage]
    public class CameraConfig
    {
        public string Alias { get; set; }

        public string DestinationFolder { get; set; }

        public string IpAddress { get; set; }

        public int PortNumber { get; set; } = 8000;

        public string UserName { get; set; }

        public string Password { get; set; }

        public bool DownloadPhotos { get; set; }

        public int? RetentionPeriodDays { get; set; } = 7;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(this.GetRow("Alias", this.Alias));
            sb.AppendLine(this.GetRow("Destination", this.DestinationFolder));
            sb.AppendLine(this.GetRow("IP Address", $"{this.IpAddress}:{this.PortNumber}"));
            sb.AppendLine(this.GetRow("User name", this.UserName));
            sb.AppendLine(this.GetRow("Retention Period", this.RetentionPeriodDays.ToString()));

            return sb.ToString();
        }

        public string ToHtmlTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("<table>");
            sb.AppendLine(this.GetHtmlRow("Alias", this.Alias));
            sb.AppendLine(this.GetHtmlRow("Destination", this.DestinationFolder));
            sb.AppendLine(this.GetHtmlRow("IP Address", $"{this.IpAddress}:{this.PortNumber}"));
            sb.AppendLine(this.GetHtmlRow("User name", this.UserName));
            sb.AppendLine(this.GetHtmlRow("Retention Period", this.RetentionPeriodDays.ToString()));
            sb.AppendLine("</table>");

            return sb.ToString();
        }

        private string GetRow(string field, string value)
        {
            return $"{field,-24}: {value}";
        }

        private string GetHtmlRow(string field, string value)
        {
            return $"<tr><td>{field}</td><td>{value}</td></tr>";
        }
    }
}

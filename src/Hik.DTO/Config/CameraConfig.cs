using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Hik.DTO.Config
{
    [ExcludeFromCodeCoverage]
    public class CameraConfig : BaseConfig
    {
        public int ProcessingPeriodHours { get; set; }

        public string IpAddress { get; set; }

        public int PortNumber { get; set; } = 8000;

        public string UserName { get; set; }

        public string Password { get; set; }

        public int? RetentionPeriodDays { get; set; } = 7;

        public ClientType ClientType { get; set; } = ClientType.HikVisionVideo; 

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(this.GetRow("Alias", this.Alias));
            sb.AppendLine(this.GetRow("Destination", this.DestinationFolder));
            sb.AppendLine(this.GetRow("IP Address", $"{this.IpAddress}:{this.PortNumber}"));
            sb.AppendLine(this.GetRow("User name", this.UserName));

            return sb.ToString();
        }

        public override string ToHtmlTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("<table>");
            sb.AppendLine(this.GetHtmlRow("Alias", this.Alias));
            sb.AppendLine(this.GetHtmlRow("Destination", this.DestinationFolder));
            sb.AppendLine(this.GetHtmlRow("IP Address", $"{this.IpAddress}:{this.PortNumber}"));
            sb.AppendLine(this.GetHtmlRow("User name", this.UserName));
            sb.AppendLine("</table>");

            return sb.ToString();
        }
    }
}

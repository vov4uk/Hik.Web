using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Hik.DTO.Config
{
    [ExcludeFromCodeCoverage]
    public class CameraConfig : BaseConfig
    {
        public int ProcessingPeriodHours { get; set; }

        public DeviceConfig Camera { get; set; } = new DeviceConfig() { PortNumber = 8000 };

        public ClientType ClientType { get; set; } = ClientType.HikVisionVideo;

        public bool SyncTime { get; set; } = true;

        public int SyncTimeDeltaSeconds { get; set; } = 5;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(this.GetRow("Alias", this.Alias));
            sb.AppendLine(this.GetRow("Destination", this.DestinationFolder));
            sb.AppendLine(this.GetRow("IP Address", $"{this.Camera.IpAddress}:{this.Camera.PortNumber}"));
            sb.AppendLine(this.GetRow("User name", this.Camera.UserName));

            return sb.ToString();
        }

        public override string ToHtmlTable()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(this.GetHtmlRow("Alias", this.Alias));
            sb.AppendLine(this.GetHtmlRow("Destination", this.DestinationFolder));
            sb.AppendLine(this.GetHtmlRow("IP Address", $"{this.Camera.IpAddress}:{this.Camera.PortNumber}"));
            sb.AppendLine(this.GetHtmlRow("User name", this.Camera.UserName));
            return sb.ToString();
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HikConsole.Config
{
    [ExcludeFromCodeCoverage]
    public class CameraConfig
    {
        public string Allias { get; set; }

        public string DestinationFolder { get; set; }

        public string IpAddress { get; set; }

        public int PortNumber { get; set; } = 8000;

        public string UserName { get; set; }

        public string Password { get; set; }

        public bool ShowProgress { get; set; } = true;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"Allias      : {this.Allias}");
            sb.AppendLine($"Destination : {this.DestinationFolder}");
            sb.AppendLine($"IP Address  : {this.IpAddress}:{this.PortNumber.ToString()}");
            sb.AppendLine($"User name   : {this.UserName}");

            return sb.ToString();
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HikConsole.Config
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

        public bool ShowProgress { get; set; } = true;

        public bool DownloadPhotos { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"Alias       : {this.Alias}");
            sb.AppendLine($"Destination : {this.DestinationFolder}");
            sb.AppendLine($"IP Address  : {this.IpAddress}:{this.PortNumber.ToString()}");
            sb.AppendLine($"User name   : {this.UserName}");

            return sb.ToString();
        }
    }
}

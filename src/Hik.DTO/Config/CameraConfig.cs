using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using FluentValidation;

namespace Hik.DTO.Config
{
    [ExcludeFromCodeCoverage]
    public class CameraConfig : BaseConfig
    {
        public int ProcessingPeriodHours { get; set; }

        public DeviceConfig Camera { get; set; }

        public ClientType ClientType { get; set; }

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

    public class CameraConfigValidator : AbstractValidator<CameraConfig>
    {
        public CameraConfigValidator()
        {
            RuleFor(x => x.ProcessingPeriodHours).GreaterThan(0);
            RuleFor(x => x.ClientType).NotEqual(ClientType.None);
            RuleFor(x => x.Camera).NotNull().SetValidator(new DeviceConfigValidator());
            RuleFor(x => x.DestinationFolder).Must(x => Directory.Exists(x));
        }
    }
}

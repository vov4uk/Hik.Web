using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using FluentValidation;

namespace Hik.DTO.Config
{
    [ExcludeFromCodeCoverage]
    public class CameraConfig : BaseConfig
    {
        [Display(Name = "Get files for last n hours")]
        public int ProcessingPeriodHours { get; set; }

        public DeviceConfig Camera { get; set; }

        [Display(Name = "Client Type")]
        public ClientType ClientType { get; set; }

        [Display(Name = "Synchronize camera time with computer")]
        public bool SyncTime { get; set; } = true;

        [Display(Name = "Synchronize camera if difference bigger than n seconds")]
        public int SyncTimeDeltaSeconds { get; set; } = 5;

        [Display(Name = "Remote path")]
        public string RemotePath { get; set; }

        // If false - will save files in tree structure {Year}-{Month}\{Day}\{Hour}
        // Else directrly to root folder
        [Display(Name = "Save files to root of destination folder?")]
        public bool SaveFilesToRootFolder { get; set; } = false;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(this.GetRow("Destination", this.DestinationFolder));
            sb.AppendLine(this.GetRow("IP Address", $"{this.Camera.IpAddress}:{this.Camera.PortNumber}"));
            sb.AppendLine(this.GetRow("User name", this.Camera.UserName));

            return sb.ToString();
        }

        public override string ToHtmlTable()
        {
            StringBuilder sb = new StringBuilder();
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
            RuleFor(x => x.DestinationFolder).NotEmpty();
        }
    }
}

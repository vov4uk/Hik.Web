using System.IO;
using FluentValidation;

namespace Hik.DTO.Config
{
    public class FtpUploaderConfig : BaseConfig
    {
        public string RemoteFolder { get; set; }

        public DeviceConfig FtpServer { get; set; }

        public string[] AllowedFileExtentions { get; set; }

        public int SkipLast { get; set; } = 0;
    }

    public class FtpUploaderConfigValidator : AbstractValidator<FtpUploaderConfig>
    {
        public FtpUploaderConfigValidator()
        {
            RuleFor(x => x.RemoteFolder).NotEmpty();
            RuleFor(x => x.DestinationFolder).Must(x => Directory.Exists(x));
            RuleFor(x => x.AllowedFileExtentions).NotEmpty();
            RuleFor(x => x.FtpServer).NotNull().SetValidator(new DeviceConfigValidator());
        }
    }
}

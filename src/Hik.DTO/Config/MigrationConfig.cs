using System.IO;
using FluentValidation;

namespace Hik.DTO.Config
{
    public class MigrationConfig : BaseConfig
    {
        public string TriggerKey { get; set; }

        public bool ReadVideoDuration { get; set; }
    }

    public class MigrationConfigValidator : AbstractValidator<MigrationConfig>
    {
        public MigrationConfigValidator()
        {
            RuleFor(x => x.DestinationFolder).Must(x => Directory.Exists(x));
        }
    }
}

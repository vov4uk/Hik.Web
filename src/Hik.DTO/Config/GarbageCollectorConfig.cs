using System.IO;
using System.Linq;
using FluentValidation;

namespace Hik.DTO.Config
{
    public class GarbageCollectorConfig : BaseConfig
    {
        public int RetentionPeriodDays { get; set; } = -1;

        public double FreeSpacePercentage { get; set; }

        public string[] TopFolders { get; set; }

        public string[] Triggers { get; set; }

        public string FileExtention { get; set; }
    }

    public class GarbageCollectorConfigValidator : AbstractValidator<GarbageCollectorConfig>
    {
        public GarbageCollectorConfigValidator()
        {
            RuleFor(x => x.FileExtention).NotEmpty();
            RuleFor(x => x.DestinationFolder).Must(x => Directory.Exists(x));
            RuleFor(x => x).Custom((x, context) =>
            {
                if (x.FreeSpacePercentage > 0 && x.RetentionPeriodDays > 0)
                {
                    context.AddFailure("Only one property allowed 'FreeSpacePercentage' or 'RetentionPeriodDays'");
                }

                if (x.FreeSpacePercentage > 0 && x.TopFolders?.Any() == false)
                {
                    context.AddFailure("TopFolders reuired then FreeSpacePercentage defined");
                }
            });
        }
    }
}

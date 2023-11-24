using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentValidation;

namespace Hik.DTO.Config
{
    public class GarbageCollectorConfig : BaseConfig
    {
        [Display(Name = "Remove files older than (days)")]
        public int RetentionPeriodDays { get; set; } = -1;

        [Display(Name = "Remove files if drive has less than n% free space")]
        public double FreeSpacePercentage { get; set; }

        [Display(Name = "Job triggers to process")]
        public int[] Triggers { get; set; } = System.Array.Empty<int>();

        [Display(Name = "File extention (.jpg)")]
        public string FileExtention { get; set; }
    }

    public class GarbageCollectorConfigValidator : AbstractValidator<GarbageCollectorConfig>
    {
        public GarbageCollectorConfigValidator()
        {
            RuleFor(x => x.FileExtention).NotEmpty();
            RuleFor(x => x.DestinationFolder).NotEmpty();
            RuleFor(x => x).Custom((x, context) =>
            {
                if (x.FreeSpacePercentage > 0 && x.RetentionPeriodDays > 0)
                {
                    context.AddFailure("Only one property allowed 'FreeSpacePercentage' or 'RetentionPeriodDays'");
                }

                if (x.FreeSpacePercentage > 0 && x.Triggers?.Length == 0)
                {
                    context.AddFailure("Triggers reuired then FreeSpacePercentage defined");
                }
            });
        }
    }
}

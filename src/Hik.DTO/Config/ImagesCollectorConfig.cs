using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Hik.DTO.Config
{
    public class ImagesCollectorConfig : BaseConfig
    {
        [Display(Name = "Source folder")]
        public string SourceFolder { get; set; }

        [Display(Name = "Sent email if processed more than n files (if 0 not sent)")]
        public int AbnormalFilesCount { get; set; } = 0;
    }

    public class ImagesConfigValidator : AbstractValidator<ImagesCollectorConfig>
    {
        public ImagesConfigValidator()
        {
            RuleFor(x => x.DestinationFolder).NotNull();
            RuleFor(x => x.SourceFolder).NotNull();
        }
    }
}

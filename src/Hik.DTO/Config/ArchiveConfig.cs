using System.IO;
using FluentValidation;

namespace Hik.DTO.Config
{
    public class ArchiveConfig : BaseConfig
    {
        public bool UnzipFiles { get; set; }

        public string SourceFolder { get; set; }

        public string FileNamePattern { get; set; } // = "{0}";

        public string FileNameDateTimeFormat { get; set; } //= "yyyyMMddHHmmssfff";

        public int SkipLast { get; set; } = 0;

        public int AbnormalFilesCount { get; set; } = 0;

        public string[] AllowedFileExtentions { get; set; } // = { ".mp4", ".jpg", ".ini" };
    }

    public class ArchiveConfigValidator : AbstractValidator<ArchiveConfig>
    {
        public ArchiveConfigValidator()
        {
            RuleFor(x => x.FileNamePattern).NotEmpty();
            RuleFor(x => x.FileNameDateTimeFormat).NotEmpty();
            RuleFor(x => x.DestinationFolder).NotNull();
            RuleFor(x => x.SourceFolder).NotNull();
            RuleFor(x => x.AllowedFileExtentions).NotEmpty();
        }
    }
}

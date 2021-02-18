namespace Hik.DTO.Config
{
    public class ArchiveConfig : BaseConfig
    {
        public string SourceFolder { get; set; }

        public string FileNamePattern { get; set; } = "{0}";

        public string FileNameDateTimeFormat { get; set; } = "yyyyMMddHHmmssfff";

        public int SkipLast { get; set; } = 0;
    }
}

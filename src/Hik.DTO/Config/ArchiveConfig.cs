namespace Hik.DTO.Config
{
    public class ArchiveConfig : BaseConfig
    {
        public string SourceFolder { get; set; }

        public string FileNamePattern { get; set; } = "{0}";

        public string FileNameDateTimeFormat { get; set; } = "yyyyMMddHHmmssfff";

        public int SkipLast { get; set; } = 0;

        public int AbnormalFilesCount { get; set; } = 0;

        public DetectPeopleConfig DetectPeopleConfig { get; set; }
    }

    public class DetectPeopleConfig
    {
        public bool DetectPeoples { get; set; } = false;

        public bool DeletePhotosWithoutPeoples { get; set; } = false;

        public string JunkFolder { get; set; }

        public RabbitMQConfig RabbitMQConfig { get; set; }
    }

    public class RabbitMQConfig
    {
        public string HostName { get; set; }
        public string QueueName { get; set; }
        public string RoutingKey { get; set; }
    }
}

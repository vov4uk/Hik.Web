namespace Hik.DTO.Config
{
    public class DetectPeopleConfig : BaseConfig
    {
        public string SourceFolder { get; set; }

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

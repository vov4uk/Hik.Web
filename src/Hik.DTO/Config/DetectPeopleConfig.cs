using System.IO;
using FluentValidation;

namespace Hik.DTO.Config
{
    public class DetectPeopleConfig : BaseConfig
    {
        public bool UnzipFiles { get; set; }

        public string SourceFolder { get; set; }

        public string JunkFolder { get; set; }

        public int SkipLast { get; set; } = 0;

        public bool CompressNewFile { get; set; } = true;

        public RabbitMQConfig RabbitMQConfig { get; set; }
    }

    public class RabbitMQConfig
    {
        public string HostName { get; set; }
        public string QueueName { get; set; }
        public string RoutingKey { get; set; }
    }

    public class DetectPeopleConfigValidator : AbstractValidator<DetectPeopleConfig>
    {
        public DetectPeopleConfigValidator()
        {
            RuleFor(x => x.DestinationFolder).Must(x => Directory.Exists(x));
            RuleFor(x => x.SourceFolder).Must(x => Directory.Exists(x));
            RuleFor(x => x.JunkFolder).Must(x => Directory.Exists(x));
            RuleFor(x => x.RabbitMQConfig).NotNull();
            RuleFor(x => x.RabbitMQConfig.QueueName).NotEmpty();
            RuleFor(x => x.RabbitMQConfig.HostName).NotEmpty();
            RuleFor(x => x.RabbitMQConfig.RoutingKey).NotEmpty();
        }
    }
}

using Hik.DTO.Config;

namespace DetectPeople.Service
{
    public class PeopleDetectConfig
    {
        public RabbitMQConfig RabbitMQ { get; set; }

        public bool DetectFaces { get; set; } = true;

        public double FaceCoeficient { get; set; } = 0.65;

        public bool OnlyInsideFaces { get; set; } = true;
    }
}

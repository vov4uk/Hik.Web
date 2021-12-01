using Hik.DTO.Config;

namespace DetectPeople.Service
{
    public class PeopleDetectConfig
    {
        public RabbitMQConfig RabbitMQ { get; set; }

        public bool DrawJunkObjects { get; set; } = true;

        public bool DetectFaces { get; set; } = true;

        public double FaceCoeficient { get; set; } = 0.65;

        public bool OnlyInsideFaces { get; set; } = true;

        public string[] AllowedObjects { get; set; } = new[] { "person" };

        public double MinPersonHeightPersentage { get; set; } = 13.1;
        public double MinPersonWidthPersentage { get; set; } = 3.7;
        public int MinPersonHeightPixel { get; set; } = 200;
        public int MinPersonWidthPixel { get; set; } = 100;
    }
}

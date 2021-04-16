namespace Hik.DTO.Config
{
    public class GarbageCollectorConfig : BaseConfig
    {
        public double FreeSpacePercentage { get; set; }

        public int BatchSize { get; set; }

        public string[] Triggers { get; set; }
    }
}

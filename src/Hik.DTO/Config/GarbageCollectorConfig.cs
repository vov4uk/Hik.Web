namespace Hik.DTO.Config
{
    public class GarbageCollectorConfig : BaseConfig
    {
        public int RetentionPeriodDays { get; set; } = -1;

        public double FreeSpacePercentage { get; set; }

        public string[] Triggers { get; set; }
    }
}

namespace Hik.DTO.Config
{
    public class GarbageCollectorConfig : BaseConfig
    {
        public int RetentionPeriodDays { get; set; } = -1;

        public double FreeSpacePercentage { get; set; }

        public string[] TopFolders { get; set; }

        public string FileExtention { get; set; } = "*.*";
    }
}

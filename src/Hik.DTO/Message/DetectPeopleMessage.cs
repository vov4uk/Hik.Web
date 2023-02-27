namespace Hik.DTO.Message
{
    public class DetectPeopleMessage
    {
        public bool DeleteJunk { get; set; }
        public string JunkFilePath { get; set; }
        public string NewFileName { get; set; }
        public string NewFilePath { get; set; }
        public string OldFilePath { get; set; }
        public string UniqueId { get; set; }

        public bool CompressNewFile { get; set; } = true;
    }
}

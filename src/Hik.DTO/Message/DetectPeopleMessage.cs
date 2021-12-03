using Newtonsoft.Json;

namespace Hik.DTO.Message
{
    public class DetectPeopleMessage
    {
        public string OldFilePath { get; set; }
        public string NewFilePath { get; set; }
        public bool DeleteJunk { get; set; }
        public string JunkFilePath { get; set; }
        public string NewFileName { get; set; }
        public string UniqueId { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

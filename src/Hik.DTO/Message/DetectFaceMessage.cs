using Newtonsoft.Json;

namespace Hik.DTO.Message
{
    public class DetectFaceMessage
    {
        public string FilePath { get; set; }

        public string FileName { get; set; }

        public int[] BBox { get; set; }

        public string PhotoId { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}

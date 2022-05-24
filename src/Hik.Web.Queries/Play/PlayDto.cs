using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Play
{
    public class PlayDto : IHandlerResult
    {
        public string FileTitle { get; set; }
        public string FileTo { get; set; }
        public string Poster { get; set; }

        public MediaFileDto PreviousFile { get; set; }

        public MediaFileDto CurrentFile { get; set; }

        public MediaFileDto NextFile { get; set; }
    }
}

using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Play
{
    public class PlayDto : IHandlerResult
    {
        public string FileTitle { get; set; }
        public string FileTo { get; set; }
        public string Poster { get; set; }

        public MediaFileDTO PreviousFile { get; set; }

        public MediaFileDTO CurrentFile { get; set; }

        public MediaFileDTO NextFile { get; set; }
    }
}

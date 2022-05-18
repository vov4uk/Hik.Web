using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Search
{
    public class SearchDto : IHandlerResult
    {
        public string Message { get; set; }

        public IReadOnlyCollection<MediaFileDTO> BeforeRange { get; set; }

        public IReadOnlyCollection<MediaFileDTO> InRange { get; set; }

        public IReadOnlyCollection<MediaFileDTO> AfterRange { get; set; }
    }
}

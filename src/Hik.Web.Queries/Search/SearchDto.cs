using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Search
{
    public class SearchDto : IHandlerResult
    {
        public string Message { get; set; }

        public IReadOnlyCollection<MediaFileDto> BeforeRange { get; set; }

        public IReadOnlyCollection<MediaFileDto> InRange { get; set; }

        public IReadOnlyCollection<MediaFileDto> AfterRange { get; set; }
    }
}

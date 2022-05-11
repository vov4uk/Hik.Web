using Hik.DTO.Contracts;

namespace Hik.Web.Queries.JobDetails
{
    public sealed class JobDetailsDto : IHandlerResult
    {
        public HikJobDto Job { get; set; }

        public List<MediaFileDTO> Files { get; set; }

        public int TotalItems { get; set; }
    }
}

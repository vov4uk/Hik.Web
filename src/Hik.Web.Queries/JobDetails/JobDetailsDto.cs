using Hik.DTO.Contracts;

namespace Hik.Web.Queries.JobDetails
{
    public sealed class JobDetailsDto : ResponseBase<MediaFileDTO>
    {
        public HikJobDto Job { get; set; }
    }
}

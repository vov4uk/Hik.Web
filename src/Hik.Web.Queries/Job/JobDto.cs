using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Job
{
    public class JobDto : ResponseBase<HikJobDto>
    {
        public string JobTriggerName { get; set; }

        public int JobTriggerId { get; set; }
    }
}

using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Job
{
    public class JobDto : ResponseBase<HikJobDto>
    {
        public string JobTriggerName { get; set; }

        public string ClassName { get; set; }

        public int JobTriggerId { get; set; }
    }
}

using MediatR;

namespace Hik.Web.Queries.JobDetails
{
    public class JobDetailsQuery : IRequest<IHandlerResult>
    {
        public int JobId { get; set; }
    }
}

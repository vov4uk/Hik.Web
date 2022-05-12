using MediatR;

namespace Hik.Web.Queries.JobDetails
{
    public class JobDetailsQuery : RequestBase, IRequest<IHandlerResult>
    {
        public int JobId { get; set; }
    }
}

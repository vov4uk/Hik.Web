using MediatR;

namespace Hik.Web.Queries.JobDetails
{
    public class JobDetailsQuery : IRequest<IHandlerResult>
    {
        public int JobId { get; set; }

        public int PageSize { get; set; }

        public int MaxPages { get; set; }

        public int CurrentPage { get; set; }
    }
}

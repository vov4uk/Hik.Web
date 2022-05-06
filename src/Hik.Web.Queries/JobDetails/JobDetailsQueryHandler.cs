using Hik.DataAccess.Abstractions;
using Hik.Web.Queries.JobDetails;

namespace Hik.Web.Queries
{
    public class JobDetailsQueryHandler : QueryHandler<JobDetailsQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public JobDetailsQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override Task<IHandlerResult> HandleAsync(JobDetailsQuery query, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

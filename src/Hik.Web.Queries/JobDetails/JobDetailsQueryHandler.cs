using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Hik.Web.Queries.JobDetails;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries
{
    public class JobDetailsQueryHandler : QueryHandler<JobDetailsQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public JobDetailsQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected async override Task<IHandlerResult> HandleAsync(JobDetailsQuery query, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var jobsRepo = uow.GetRepository<HikJob>();

                var job = await jobsRepo.FindByAsync(x => x.Id == query.JobId, x => x.ExceptionLog);

                if (job != null)
                {
                    var filesRepo = uow.GetRepository<MediaFile>();

                    var totalItems = await filesRepo.CountAsync(x => x.JobId == query.JobId);

                    var files = await filesRepo.FindManyByAscAsync(
                        predicate: x => x.JobId == query.JobId,
                        orderByAsc: x => x.Id,
                        skip: Math.Max(0, query.CurrentPage - 1) * query.PageSize,
                        top: query.PageSize);

                    return new JobDetailsDto()
                    {
                        Job = HikDatabase.Mapper.Map<HikJob, HikJobDto>(job),
                        TotalItems = totalItems,
                        Items = files.OrderBy(x => x.Date).ToList().ConvertAll(HikDatabase.Mapper.Map <MediaFile, MediaFileDto>),
                    };
                }

                return default(JobDetailsDto);
            }
        }
    }
}

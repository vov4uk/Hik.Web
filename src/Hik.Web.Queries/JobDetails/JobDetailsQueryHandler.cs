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

                var job = await jobsRepo.FindByAsync(x => x.Id == query.JobId, x => x.ExceptionLog, x => x.JobTrigger);

                if (job != null)
                {
                    var filesRepo = uow.GetRepository<MediaFile>();
                    var downloadRepo = uow.GetRepository<DownloadHistory>();

                    var totalItems = await downloadRepo.CountAsync(x => x.JobId == query.JobId);

                    var files = await filesRepo.FindManyByDescAsync(
                        x => (x.DownloadHistory == null ? 0 : x.DownloadHistory.JobId) == query.JobId,
                        x => x.Id,
                        Math.Max(0, query.CurrentPage - 1) * query.PageSize,
                        query.PageSize,
                        x => x.DownloadHistory,
                        x => x.DownloadDuration);

                    return new JobDetailsDto()
                    {
                        Job = HikDatabase.Mapper.Map<HikJob, HikJobDto>(job),
                        TotalItems = totalItems,
                        Items = files.ConvertAll(x => HikDatabase.Mapper.Map <MediaFile, MediaFileDto>(x)),
                    };
                }

                return default(JobDetailsDto);
            }
        }
    }
}

using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries.Job
{
    public class JobQueryHandler : QueryHandler<JobQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public JobQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override async Task<IHandlerResult> HandleAsync(JobQuery request, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();
                var trigger = await triggerRepo.FindByAsync(x => x.Id == request.JobTriggerId);

                if (trigger != null)
                {
                    var jobsRepo = uow.GetRepository<HikJob>();

                    var totalItems = await jobsRepo.CountAsync(x => x.JobTriggerId == request.JobTriggerId);

                    var jobs = await jobsRepo.FindManyByDescAsync(x => x.JobTriggerId == request.JobTriggerId,
                        x => x.Id,
                        Math.Max(0, request.CurrentPage - 1) * request.PageSize,
                        request.PageSize);

                    return new JobDto()
                    {
                        JobTriggerId = request.JobTriggerId,
                        JobTriggerName = trigger.TriggerKey,
                        TotalItems = totalItems,
                        Items = jobs.ConvertAll(x => HikDatabase.Mapper.Map<HikJob, HikJobDto>(x)),
                    };
                }

                return default(JobDto);
            }
        }
    }
}

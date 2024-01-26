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
                var trigger = await triggerRepo.FindByIdAsync(request.JobTriggerId);

                if (trigger != null)
                {
                    var jobsRepo = uow.GetRepository<HikJob>();

                    var totalItems = await jobsRepo.CountAsync(x => x.JobTriggerId == request.JobTriggerId);

                    var jobs = await jobsRepo.FindManyByDescAsync(
                        predicate: x => x.JobTriggerId == request.JobTriggerId,
                        orderByDesc : x => x.Id,
                        skip: Math.Max(0, request.CurrentPage - 1) * request.PageSize,
                        take: request.PageSize);

                    return new JobDto()
                    {
                        JobTriggerId = request.JobTriggerId,
                        JobTriggerName = trigger.TriggerKey,
                        ClassName = trigger.ClassName,
                        TotalItems = totalItems,
                        Items = jobs.ConvertAll(HikDatabase.Mapper.Map<HikJob, HikJobDto>),
                    };
                }

                return default(JobDto);
            }
        }
    }
}

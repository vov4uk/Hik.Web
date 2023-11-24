using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries.DashboardDetails
{
    public class DashboardDetailsQueryHandler : QueryHandler<DashboardDetailsQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public DashboardDetailsQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected async override Task<IHandlerResult> HandleAsync(DashboardDetailsQuery query, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var statRepo = uow.GetRepository<DailyStatistic>();
                var jobTriggerRepo = uow.GetRepository<JobTrigger>();

                var trigger = await jobTriggerRepo.FindByIdAsync(query.JobTriggerId);

                if (trigger != null)
                {
                    var totalItems = await statRepo.CountAsync(x => x.JobTriggerId == query.JobTriggerId);

                    var files = await statRepo.FindManyByDescAsync(
                        x => x.JobTriggerId == query.JobTriggerId,
                        x => x.Period,
                        Math.Max(0, query.CurrentPage - 1) * query.PageSize,
                        query.PageSize);

                    return new DashboardDetailsDto()
                    {
                        JobTriggerId = query.JobTriggerId,
                        JobTriggerName = trigger.TriggerKey,
                        TotalItems = totalItems,
                        Items = files.ConvertAll(HikDatabase.Mapper.Map<DailyStatistic, DailyStatisticDto>),
                    };
                }
                return default(DashboardDetailsDto);
            }
        }
    }
}

using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;

namespace Hik.Web.Queries.Statistic
{
    public class StatisticQueryHandler : QueryHandler<StatisticQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public StatisticQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected async override Task<IHandlerResult> HandleAsync(StatisticQuery query, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork())
            {
                var statRepo = uow.GetRepository<DailyStatistic>();
                var jobTriggerRepo = uow.GetRepository<JobTrigger>();

                var trigger = await jobTriggerRepo.FindByAsync(x => x.Id == query.TriggerId);

                if (trigger != null)
                {
                    var totalItems = await statRepo.CountAsync(x => x.JobTriggerId == query.TriggerId);

                    var files = await statRepo.FindManyAsync(
                        x => x.JobTriggerId == query.TriggerId,
                        x => x.Period,
                        Math.Max(0, query.CurrentPage - 1) * query.PageSize,
                        query.PageSize);

                    return new StatisticDto()
                    {
                        JobTriggerId = query.TriggerId,
                        JobTriggerName = trigger.TriggerKey,
                        TotalItems = totalItems,
                        Days = files.ConvertAll(x => HikDatabase.Mapper.Map<DailyStatistic, DailyStatisticDto>(x)),
                    };
                }
                return default(StatisticDto);
            }
        }
    }
}

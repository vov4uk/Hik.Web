using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries.Dashboard
{
    public class DashboardQueryHandler : QueryHandler<DashboardQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public DashboardQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override async Task<IHandlerResult> HandleAsync(DashboardQuery request, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();

                var filesRepo = uow.GetRepository<MediaFile>();
                var jobRepo = uow.GetRepository<HikJob>();

                var jobTriggers = await triggerRepo.GetAllAsync();

                List<DailyStatistic> statistics = await triggerRepo
                    .GetAll(x => x.DailyStatistics)
                    .Where(x => x.DailyStatistics.Any())
                    .Select(x => x.DailyStatistics.OrderByDescending(y => y.Period).First())
                    .ToListAsync(cancellationToken);

                Dictionary<int, DateTime> latestFiles = await filesRepo
                    .GetAll()
                    .GroupBy(x => x.JobTriggerId)
                    .Select(x => new KeyValuePair<int, DateTime>(x.Key, x.Max(y => y.Date)))
                    .ToDictionaryAsync(x => x.Key, y => y.Value, cancellationToken);

                var latestPeriodEnd = await jobRepo
                    .GetAll()
                    .Where(x => x.Finished != null)
                    .GroupBy(x => x.JobTriggerId)
                    .Select(x => new KeyValuePair<int, DateTime>(x.Key, x.Max(y => y.PeriodEnd ?? new DateTime())))
                    .ToListAsync(cancellationToken);

                foreach (var period in latestPeriodEnd)
                {
                    if (!latestFiles.ContainsKey(period.Key))
                    {
                        latestFiles.Add(period.Key, period.Value);
                    }
                }

                return new DashboardDto
                {
                    Files = latestFiles,
                    Triggers = jobTriggers.ConvertAll(x => HikDatabase.Mapper.Map<JobTrigger, TriggerDTO>(x)),
                    Items = statistics.ConvertAll(x => HikDatabase.Mapper.Map<DailyStatistic, DailyStatisticDto>(x))
                };
            }
        }
    }
}

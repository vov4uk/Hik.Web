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

                List<JobTrigger> jobTriggers = await triggerRepo.GetAllAsync();

                List<DailyStatistic> statistics = triggerRepo
                    .GetAll(x => x.DailyStatistics)
                    .Where(x => x.DailyStatistics.Any())
                    .Select(x => x.DailyStatistics.OrderByDescending(y => y.Period).First())
                    .ToList();

                Dictionary<int, DateTime> latestFiles = filesRepo
                    .GetAll()
                    .GroupBy(x => x.JobTriggerId)
                    .Select(x => new KeyValuePair<int, DateTime>(x.Key, x.Max(y => y.Date)))
                    .ToDictionary(x => x.Key, y => y.Value);

                List<KeyValuePair<int, DateTime>> latestFinishedJobs = jobRepo
                    .GetAll()
                    .Where(x => x.Finished != null)
                    .GroupBy(x => x.JobTriggerId)
                    .Select(x => new KeyValuePair<int, DateTime>(x.Key, x.Max(y => y.PeriodEnd ?? new DateTime())))
                    .ToList();

                foreach (var period in latestFinishedJobs)
                {
                    if (!latestFiles.ContainsKey(period.Key))
                    {
                        latestFiles.Add(period.Key, period.Value);
                    }
                }

                return new DashboardDto
                {
                    Files = latestFiles,
                    Triggers = jobTriggers.ConvertAll(x => HikDatabase.Mapper.Map<JobTrigger, TriggerDto>(x)),
                    DailyStatistics = statistics.ConvertAll(x => HikDatabase.Mapper.Map<DailyStatistic, DailyStatisticDto>(x))
                };
            }
        }
    }
}

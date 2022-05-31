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
                var statRepo = uow.GetRepository<DailyStatistic>();
                var jobRepo = uow.GetRepository<HikJob>();

                List<JobTrigger> jobTriggers = await triggerRepo.GetAllAsync();

                var statistics = await statRepo.GetLatestGroupedBy(p => p.JobTriggerId);

                var latestFiles = await filesRepo.GetLatestGroupedBy(p => p.JobTriggerId);

                var dict = latestFiles.ToDictionary(p => p.JobTriggerId, p => p.Date);

                var latestFinishedJobs = await jobRepo.GetLatestGroupedBy(
                    y => y.PeriodEnd != null,
                    x => x.JobTriggerId);

                foreach (var job in latestFinishedJobs)
                {
                    if (!dict.ContainsKey(job.JobTriggerId))
                    {
                        dict.Add(job.JobTriggerId, job.PeriodEnd.Value);
                    }
                }

                return new DashboardDto
                {
                    Files = dict,
                    Triggers = jobTriggers.ConvertAll(x => HikDatabase.Mapper.Map<JobTrigger, TriggerDto>(x)),
                    DailyStatistics = statistics.Where(x => x != null).ToList().ConvertAll(x => HikDatabase.Mapper.Map<DailyStatistic, DailyStatisticDto>(x))
                };
            }
        }
    }
}

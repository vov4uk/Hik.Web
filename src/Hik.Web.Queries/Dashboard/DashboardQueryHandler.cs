using System.Diagnostics;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;
using Serilog;

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
            var timer = new Stopwatch();
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();
                var filesRepo = uow.GetRepository<MediaFile>();
                var statRepo = uow.GetRepository<DailyStatistic>();
                var jobRepo = uow.GetRepository<HikJob>();

                timer.Restart();
                List<JobTrigger> jobTriggers = await triggerRepo.GetAllAsync();
                timer.Stop();
                Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "jobTriggers", timer.ElapsedMilliseconds);


                timer.Restart();
                var statistics = await statRepo.GetLatestGroupedBy(
                    predicate: x => x.Period == request.Day,
                    groupBy: p => p.JobTriggerId);
                timer.Stop();
                Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "statistics", timer.ElapsedMilliseconds);

                timer.Restart();
                var latestFiles = await filesRepo.GetLatestGroupedBy(
                    predicate: x => x.Date.Date == request.Day,
                    groupBy: p => p.JobTriggerId);
                timer.Stop();
                Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "latestFiles", timer.ElapsedMilliseconds);

                var dict = latestFiles.ToDictionary(p => p.JobTriggerId, p => p.Date.AddSeconds(p.Duration ?? 0));

                timer.Restart();
                var latestFinishedJobs = await jobRepo.GetLatestGroupedBy(
                    x =>
                    x.PeriodStart != null
                    && x.PeriodStart.Value.Date == request.Day
                    && x.PeriodEnd != null,
                    x => x.JobTriggerId);
                timer.Stop();
                Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "latestFinishedJobs", timer.ElapsedMilliseconds);

                foreach (var job in latestFinishedJobs)
                {
                    if (!dict.ContainsKey(job.JobTriggerId))
                    {
                        dict.Add(job.JobTriggerId, job.PeriodEnd.Value);
                    }
                }

                foreach (var trigger in jobTriggers.Where(x => x.ClassName?.Contains("GarbageCollectorJob") == false))
                {
                    trigger.LastSync = latestFinishedJobs.FirstOrDefault(x => x.JobTriggerId == trigger.Id)?.PeriodEnd;
                }


                return new DashboardDto
                {
                    Files = dict,
                    Triggers = jobTriggers.Where(x => x.IsEnabled).ToList().ConvertAll(HikDatabase.Mapper.Map<JobTrigger, TriggerDto>),
                    DailyStatistics = statistics.Where(x => x != null).ToList().ConvertAll(HikDatabase.Mapper.Map<DailyStatistic, DailyStatisticDto>)
                };
            }
        }
    }
}

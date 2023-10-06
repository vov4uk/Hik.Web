using System.Diagnostics;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Serilog;

namespace Hik.Web.Queries.JobTriggers
{
    public class JobTriggersQueryHandler : QueryHandler<JobTriggersQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public JobTriggersQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override Task<IHandlerResult> HandleAsync(JobTriggersQuery request, CancellationToken cancellationToken)
        {
            var timer = new Stopwatch();
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();

                timer.Restart();
                var triggers = triggerRepo.GetAll(x => x.LastExecutedJob).ToList();
                timer.Stop();
                Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "triggers", timer.ElapsedMilliseconds);

                //timer.Restart();
                //var jobsQuery = triggerRepo.GetAll(x => x.Jobs)
                //    .Select(x => x.Jobs.OrderByDescending(y => y.Id).FirstOrDefault())
                //    .Where(x => x != null);

                //List<HikJob> jobs = await jobsQuery.ToListAsync();

                //timer.Stop();
                //Log.Information("Query: {type}; Method {method} SQL: {sql};", this.GetType().Name, "jobs", jobsQuery.ToQueryString());
                //Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "jobs", timer.ElapsedMilliseconds);

                var items = new List<TriggerDto>();

                timer.Restart();
                foreach (var trigger in triggers)
                {
                    var triggerDto = HikDatabase.Mapper.Map<JobTrigger, TriggerDto>(trigger);
                    var latestJob = trigger.LastExecutedJob;
                    if (latestJob != null)
                    {
                        triggerDto.LastJob = HikDatabase.Mapper.Map<HikJob, HikJobDto>(latestJob);
                    }
                    items.Add(triggerDto);
                }
                timer.Stop();
                Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "items", timer.ElapsedMilliseconds);

                return Task.FromResult<IHandlerResult>(new JobTriggersDto()
                {
                    Items = items
                });
            }
        }
    }
}

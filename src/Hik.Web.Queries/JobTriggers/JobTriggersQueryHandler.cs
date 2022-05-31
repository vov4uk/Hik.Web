using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries.JobTriggers
{
    public class JobTriggersQueryHandler : QueryHandler<JobTriggersQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public JobTriggersQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override async Task<IHandlerResult> HandleAsync(JobTriggersQuery request, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();
                var triggers = await triggerRepo.GetAllAsync();

                var jobs = triggerRepo.GetAll(x => x.Jobs)
                    .Select(x => x.Jobs.OrderByDescending(y => y.Started).FirstOrDefault())
                    .Where(x => x != null)
                    .ToList();

                var items = new List<TriggerDto>();

                foreach (var trigger in triggers)
                {
                    var triggerDto = HikDatabase.Mapper.Map<JobTrigger, TriggerDto>(trigger);
                    var latestJob = jobs.FirstOrDefault(x => x.JobTriggerId == trigger.Id);
                    if (latestJob != null)
                    {
                        triggerDto.LastJob = HikDatabase.Mapper.Map<HikJob, HikJobDto>(latestJob);
                    }
                    items.Add(triggerDto);
                }

                return new JobTriggersDto()
                {
                    Items = items
                };
            }
        }
    }
}

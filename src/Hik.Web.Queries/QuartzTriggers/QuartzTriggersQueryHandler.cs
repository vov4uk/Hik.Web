using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries.QuartzTriggers
{
    public class QuartzTriggersQueryHandler : QueryHandler<QuartzTriggersQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public QuartzTriggersQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override async Task<IHandlerResult> HandleAsync(QuartzTriggersQuery request, CancellationToken cancellationToken)
        {
            List<JobTrigger> triggers;

            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var repo = uow.GetRepository<JobTrigger>();
                if (request.ActiveOnly)
                {
                    if (request.IncludeLastJob)
                    {
                        triggers = await repo.FindManyAsync(x => x.IsEnabled, x => x.LastExecutedJob);
                    }
                    else
                    {
                        triggers = await repo.FindManyAsync(x => x.IsEnabled);
                    }
                }
                else
                {
                    if (request.IncludeLastJob)
                    {
                        triggers = repo.GetAll(x => x.LastExecutedJob).ToList();
                    }
                    else
                    {
                        triggers = await repo.GetAllAsync();
                    }
                }
            }

            var items = new List<TriggerDto>();

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


            return new QuartzTriggersDto()
            {
                Triggers = items
            };
        }
    }
}

using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.Quartz.Contracts;
using Hik.Quartz.Services;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries.QuartzTriggers
{
    public class QuartzTriggersQueryHandler : QueryHandler<QuartzTriggersQuery>
    {
        private readonly ICronService cronHelper;
        private readonly IUnitOfWorkFactory factory;

        public QuartzTriggersQueryHandler(ICronService cronHelper, IUnitOfWorkFactory factory)
        {
            this.cronHelper = cronHelper;
            this.factory = factory;
        }

        protected override async Task<IHandlerResult> HandleAsync(QuartzTriggersQuery request, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<CronDto> cronTriggers = await this.cronHelper.GetAllTriggersAsync();

            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var repo = uow.GetRepository<JobTrigger>();

                var triggers = await repo.GetAllAsync();
            }

            return new QuartzTriggersDto() { Items = cronTriggers };
        }
    }
}

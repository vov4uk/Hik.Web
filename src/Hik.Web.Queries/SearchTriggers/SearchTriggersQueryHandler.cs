using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Microsoft.EntityFrameworkCore;

namespace Hik.Web.Queries.Search
{
    public class SearchTriggersQueryHandler : QueryHandler<SearchTriggersQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public SearchTriggersQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override async Task<IHandlerResult> HandleAsync(SearchTriggersQuery request, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var repo = uow.GetRepository<JobTrigger>();

                var triggers = await repo.FindManyAsync(x => x.ShowInSearch);

                return new SearchTriggersDto
                {
                    Triggers = triggers.OrderBy(x => x.TriggerKey).ToDictionary(k => k.Id, v => v.TriggerKey)
                };
            }
        }
    }
}

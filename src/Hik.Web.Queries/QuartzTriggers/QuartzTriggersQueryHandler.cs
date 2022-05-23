using Hik.Quartz.Services;

namespace Hik.Web.Queries.QuartzTriggers
{
    public class QuartzTriggersQueryHandler : QueryHandler<QuartzTriggersQuery>
    {
        private readonly ICronService cronHelper;

        public QuartzTriggersQueryHandler(ICronService cronHelper)
        {
            this.cronHelper = cronHelper;
        }

        protected override async Task<IHandlerResult> HandleAsync(QuartzTriggersQuery request, CancellationToken cancellationToken)
        {
            var triggers = await this.cronHelper.GetAllCronsAsync();
            return new QuartzTriggersDto() { Items = triggers };
        }
    }
}

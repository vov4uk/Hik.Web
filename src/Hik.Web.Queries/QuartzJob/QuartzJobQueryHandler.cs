using Hik.Quartz.Services;
using Microsoft.Extensions.Configuration;

namespace Hik.Web.Queries.QuartzJob
{
    public class QuartzJobQueryHandler : QueryHandler<QuartzJobQuery>
    {
        private readonly ICronService cronHelper;
        private readonly IConfiguration configuration;

        public QuartzJobQueryHandler(IConfiguration configuration, ICronService cronHelper)
        {
            this.configuration = configuration;
            this.cronHelper = cronHelper;
        }

        protected override async Task<IHandlerResult> HandleAsync(QuartzJobQuery request, CancellationToken cancellationToken)
        {
            var cron = await cronHelper.GetCronAsync(configuration, request.Name, request.Group);

            if (cron != null)
            {
                return new QuartzJobDto { Cron = cron };
            }

            return default(QuartzJobDto);
        }
    }
}

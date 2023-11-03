using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Hik.Quartz;
using Hik.Quartz.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Hik.Web.Commands.Cron
{
    public class RestartSchedulerCommandHandler : IRequestHandler<RestartSchedulerCommand>
    {
        private readonly ICronService cronHelper;
        private readonly IConfiguration configuration;
        private readonly IUnitOfWorkFactory unitOfWorkFactory;

        public RestartSchedulerCommandHandler(IConfiguration configuration, ICronService cronHelper, IUnitOfWorkFactory unitOfWorkFactory)
        {
            this.configuration = configuration;
            this.cronHelper = cronHelper;
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        public async Task Handle(RestartSchedulerCommand request, CancellationToken cancellationToken)
        {
            List<JobTrigger> triggers = new List<JobTrigger>();

            using (var uow = unitOfWorkFactory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var repo = uow.GetRepository<JobTrigger>();
                triggers = await repo.FindManyAsync(x => x.IsEnabled);
            }

            var items = triggers.Select(HikDatabase.Mapper.Map<JobTrigger, TriggerDto>);

            var validTriggers = items.Where(x =>
            !string.IsNullOrEmpty(x.Name) &&
            !string.IsNullOrEmpty(x.Group) &&
            !string.IsNullOrEmpty(x.CronExpression) &&
            !string.IsNullOrEmpty(x.Description))
                .Select(x => x.ToCron())
                .ToList();

            QuartzStartup.InitializeJobs(configuration, validTriggers);

            await cronHelper.RestartSchedulerAsync(configuration);
        }
    }
}

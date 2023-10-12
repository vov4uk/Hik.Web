using Hik.Quartz.Services;
using Job;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Activity = Job.Activity;

namespace Hik.Web.Commands.Cron
{
    public class CronCommandHandler : IRequestHandler<RestartSchedulerCommand>,
        IRequestHandler<UpdateQuartzJobCommand>,
        IRequestHandler<DeleteQuartzJobCommand>,
        IRequestHandler<StartActivityCommand, int>
    {
        private readonly ICronService cronHelper;
        private readonly IConfiguration configuration;

        public CronCommandHandler(IConfiguration configuration, ICronService cronHelper)
        {
            this.configuration = configuration;
            this.cronHelper = cronHelper;
        }

        public async Task Handle(RestartSchedulerCommand request, CancellationToken cancellationToken)
        {
            await cronHelper.RestartSchedulerAsync(configuration);
        }

        public async Task Handle(UpdateQuartzJobCommand request, CancellationToken cancellationToken)
        {
            await cronHelper.UpdateTriggerAsync(configuration, request.Cron);
        }

        public async Task Handle(DeleteQuartzJobCommand request, CancellationToken cancellationToken)
        {
            await cronHelper.DeleteTriggerAsync(configuration, request.Group, request.Name, request.ClassName);
        }

        public async Task<int> Handle(StartActivityCommand request, CancellationToken cancellationToken)
        {
            var trigger = await cronHelper.GetCronAsync(configuration, request.Name, request.Group);
            if (trigger != null)
            {
                bool runAsTask = Debugger.IsAttached || trigger.RunAsTask;
                var connectionString = configuration.GetSection("DBConfiguration").GetSection("ConnectionString").Value;

                var parameters = new Parameters(trigger.ClassName, trigger.Group, trigger.Name, trigger.ConfigPath, connectionString, runAsTask);

                var activity = new Activity(parameters);
                await activity.Start();
                return activity.ProcessId;
            }
            return 0;
        }
    }
}

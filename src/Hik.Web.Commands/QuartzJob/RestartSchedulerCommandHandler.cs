using Hik.Quartz.Services;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Hik.Web.Commands.QuartzJob
{
    public class RestartSchedulerCommandHandler : IRequestHandler<RestartSchedulerCommand>
    {
        private readonly ICronService cronHelper;
        private readonly IConfiguration configuration;

        public RestartSchedulerCommandHandler(IConfiguration configuration, ICronService cronHelper)
        {
            this.configuration = configuration;
            this.cronHelper = cronHelper;
        }

        public async Task<Unit> Handle(RestartSchedulerCommand request, CancellationToken cancellationToken)
        {
            await cronHelper.RestartSchedulerAsync(configuration);
            return Unit.Value;
        }
    }
}

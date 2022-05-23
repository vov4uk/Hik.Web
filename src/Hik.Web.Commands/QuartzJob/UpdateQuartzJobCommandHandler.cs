using Hik.Quartz.Services;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Hik.Web.Commands.QuartzJob
{
    public class UpdateQuartzJobCommandHandler : IRequestHandler<UpdateQuartzJobCommand>
    {
        private readonly ICronService cronHelper;
        private readonly IConfiguration configuration;

        public UpdateQuartzJobCommandHandler(IConfiguration configuration, ICronService cronHelper)
        {
            this.configuration = configuration;
            this.cronHelper = cronHelper;
        }

        public async Task<Unit> Handle(UpdateQuartzJobCommand request, CancellationToken cancellationToken)
        {
            await cronHelper.UpdateCronAsync(configuration, request.Cron);
            return Unit.Value;
        }
    }
}

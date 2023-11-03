using Job;
using MediatR;
using Activity = Job.Activity;

namespace Hik.Web.Commands.Cron
{
    public class StartActivityCommandHandler : IRequestHandler<StartActivityCommand>
    {
        public Task Handle(StartActivityCommand request, CancellationToken cancellationToken)
        {
            var parameters = new Parameters(request.Group, request.Name, request.AppConfigsPath, request.Environment);

            var activity = new Activity(parameters);
            Task.Run(activity.Start);

            return Task.CompletedTask;
        }
    }
}

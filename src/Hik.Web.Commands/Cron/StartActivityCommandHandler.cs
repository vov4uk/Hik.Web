using Hik.DataAccess;
using Job;
using MediatR;
using Microsoft.Extensions.Configuration;
using Activity = Job.Activity;

namespace Hik.Web.Commands.Cron
{
    public class StartActivityCommandHandler : IRequestHandler<StartActivityCommand>
    {
        private readonly IConfiguration _configuration;
        public StartActivityCommandHandler(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task Handle(StartActivityCommand request, CancellationToken cancellationToken)
        {
            var parameters = new Parameters(request.Group, request.Name, request.Environment);
            var connection = _configuration.GetSection("DBConfiguration").Get<DbConfiguration>();
            var activity = new Activity(parameters, connection, request.WorkingDirectory);
            Task.Run(activity.Start);

            return Task.CompletedTask;
        }
    }
}

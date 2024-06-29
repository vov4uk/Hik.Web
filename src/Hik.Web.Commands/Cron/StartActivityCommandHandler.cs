using Hik.DataAccess;
using Hik.Helpers.Email;
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
            var email = _configuration.GetSection("EmailConfig").Get<EmailConfig>();
            var activity = new Activity(parameters, connection, email, request.WorkingDirectory);
            Task.Run(activity.Start, cancellationToken);

            return Task.CompletedTask;
        }
    }
}

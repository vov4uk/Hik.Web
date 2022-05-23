using Hik.Web.Commands.Activity;
using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace Job.Commands
{
    [ExcludeFromCodeCoverage]
    public class StartActivityCommandHandler : IRequestHandler<StartActivityCommand, int>
    {
        public async Task<int> Handle(StartActivityCommand request, CancellationToken cancellationToken)
        {
            var activity = new Activity(request.Parameters);
            await activity.Start();
            return activity.ProcessId;
        }
    }
}

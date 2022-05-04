using MediatR;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Job.Commands
{
    [ExcludeFromCodeCoverage]
    public class ActivityCommandHandler : IRequestHandler<ActivityCommand, int>
    {
        public async Task<int> Handle(ActivityCommand request, CancellationToken cancellationToken)
        {
            var activity = new Activity(request.Model);
            await activity.Start();
            return activity.ProcessId;
        }
    }
}

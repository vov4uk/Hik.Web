using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace Job.Commands
{
    [ExcludeFromCodeCoverage]
    public class ActivityCommand : IRequest<int>
    {
        public Parameters Model { get; }

        public ActivityCommand(Parameters model)
        {
            Model = model;
        }
    }
}

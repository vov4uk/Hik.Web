using MediatR;

namespace Job.Commands
{
    public class ActivityCommand : IRequest<int>
    {
        public Parameters Model { get; }

        public ActivityCommand(Parameters model)
        {
            Model = model;
        }
    }
}

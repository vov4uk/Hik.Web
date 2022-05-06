using Job;
using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace Hik.Web.Commands.Activity
{
    [ExcludeFromCodeCoverage]
    public class ActivityCommand : IRequest<int>
    {
        public Parameters Parameters { get; }

        public ActivityCommand(Parameters parameters)
        {
            Parameters = parameters;
        }
    }
}

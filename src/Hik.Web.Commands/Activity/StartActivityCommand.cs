using Job;
using MediatR;
using System.Diagnostics.CodeAnalysis;

namespace Hik.Web.Commands.Activity
{
    [ExcludeFromCodeCoverage]
    public class StartActivityCommand : IRequest<int>
    {
        public Parameters Parameters { get; }

        public StartActivityCommand(Parameters parameters)
        {
            Parameters = parameters;
        }
    }
}

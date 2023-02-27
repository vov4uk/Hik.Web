using Hik.Helpers.Abstraction;
using MediatR;

namespace Hik.Web.Commands.Config
{
    public class UpdateQuartzJobConfigCommandHandler : IRequestHandler<UpdateQuartzJobConfigCommand>
    {
        private readonly IFilesHelper fileHelper;

        public UpdateQuartzJobConfigCommandHandler(IFilesHelper filesHelper)
        {
            fileHelper = filesHelper;
        }

        public Task Handle(UpdateQuartzJobConfigCommand request, CancellationToken cancellationToken)
        {
            fileHelper.WriteAllText(request.Path, request.Json);
            return Task.CompletedTask;
        }
    }
}

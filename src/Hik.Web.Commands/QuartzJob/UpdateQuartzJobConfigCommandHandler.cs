using Hik.Helpers.Abstraction;
using MediatR;

namespace Hik.Web.Commands.QuartzJob
{
    public class UpdateQuartzJobConfigCommandHandler : IRequestHandler<UpdateQuartzJobConfigCommand>
    {
        private readonly IFilesHelper fileHelper;

        public UpdateQuartzJobConfigCommandHandler(IFilesHelper cronHelper)
        {
            this.fileHelper = cronHelper;
        }

        public Task<Unit> Handle(UpdateQuartzJobConfigCommand request, CancellationToken cancellationToken)
        {
            fileHelper.WriteAllText(request.Path, request.Json);
            return Task.FromResult(Unit.Value);
        }
    }
}

using MediatR;

namespace Hik.Web.Queries
{
    public abstract class QueryHandler<TRequest> : IRequestHandler<TRequest, IHandlerResult>
        where TRequest : IRequest<IHandlerResult>
    {
        public async Task<IHandlerResult> Handle(TRequest request, CancellationToken cancellationToken)
        {
            IHandlerResult canHandle = await this.CanHandleAsync(request, cancellationToken);
            if (canHandle == null)
            {
                return await this.HandleAsync(request, cancellationToken);
            }

            return canHandle;
        }

        protected virtual Task<IHandlerResult> CanHandleAsync(TRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult<IHandlerResult>(null);
        }

        protected abstract Task<IHandlerResult> HandleAsync(TRequest request, CancellationToken cancellationToken);
    }
}

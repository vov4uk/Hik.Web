using MediatR;

namespace Hik.Web.Queries
{
    public abstract class QueryHandler<TRequest> : IRequestHandler<TRequest, IHandlerResult>
        where TRequest : IRequest<IHandlerResult>
    {
        public async Task<IHandlerResult> Handle(TRequest query, CancellationToken cancellationToken)
        {
            IHandlerResult canHandle = await this.CanHandleAsync(query, cancellationToken);
            if (canHandle == null)
            {
                return await this.HandleAsync(query, cancellationToken);
            }

            return canHandle;
        }

        protected virtual Task<IHandlerResult> CanHandleAsync(TRequest query, CancellationToken cancellationToken)
        {
            return Task.FromResult<IHandlerResult>(null);
        }

        protected abstract Task<IHandlerResult> HandleAsync(TRequest query, CancellationToken cancellationToken);

    }
}

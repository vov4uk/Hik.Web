using System.Diagnostics;
using MediatR;
using Newtonsoft.Json;
using Serilog;

namespace Hik.Web.Queries
{
    public abstract class QueryHandler<TRequest> : IRequestHandler<TRequest, IHandlerResult>
        where TRequest : IRequest<IHandlerResult>
    {
        private readonly ILogger logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("Logs\\QueryHandler_.txt", rollingInterval: RollingInterval.Day)
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

        private readonly Stopwatch timer = new();

        public async Task<IHandlerResult> Handle(TRequest request, CancellationToken cancellationToken)
        {
            IHandlerResult canHandle = await this.CanHandleAsync(request, cancellationToken);
            if (canHandle == null)
            {
                timer.Restart();
                var result = await this.HandleAsync(request, cancellationToken);
                timer.Stop();
                logger.Information("Query: {type}; Duration: {duration}ms; Body: {body}", this.GetType().Name, timer.ElapsedMilliseconds, JsonConvert.SerializeObject(request));
                return result;
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

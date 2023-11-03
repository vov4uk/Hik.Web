using System.Diagnostics;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Hik.Web.Queries.QuartzTrigger
{
    public class QuartzTriggerQueryHandler : QueryHandler<QuartzTriggerQuery>
    {
        private readonly IUnitOfWorkFactory factory;

        public QuartzTriggerQueryHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        protected override async Task<IHandlerResult> HandleAsync(QuartzTriggerQuery request, CancellationToken cancellationToken)
        {
            var timer = new Stopwatch();
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();

                timer.Restart();
                var trigger = await triggerRepo.FindByAsync(x => x.Id == request.Id);
                timer.Stop();
                Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "trigger", timer.ElapsedMilliseconds);

                TriggerDto triggerDto = null;
                if (trigger != null)
                {
                    triggerDto = HikDatabase.Mapper.Map<JobTrigger, TriggerDto>(trigger);
                }

                return new QuartzTriggerDto()
                {
                     Trigger = triggerDto
                };
            }
        }
    }
}

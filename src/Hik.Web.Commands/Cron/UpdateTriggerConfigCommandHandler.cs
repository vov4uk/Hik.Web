using System.Diagnostics;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Hik.Web.Commands.Cron
{
    public class UpdateTriggerConfigCommandHandler : IRequestHandler<UpdateTriggerConfigCommand>
    {
        private readonly IUnitOfWorkFactory factory;

        public UpdateTriggerConfigCommandHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }
        public async Task Handle(UpdateTriggerConfigCommand request, CancellationToken cancellationToken)
        {
            var timer = new Stopwatch();
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();

                timer.Restart();
                var trigger = await triggerRepo.FindByAsync(x => x.Id == request.TriggerId);
                timer.Stop();
                Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "trigger", timer.ElapsedMilliseconds);

                if (trigger != null)
                {
                    trigger.Config = request.JsonConfig;
                    triggerRepo.Update(trigger);
                }

                //uow.SaveChanges();
            }
        }
    }
}

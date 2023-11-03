using System.Diagnostics;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Hik.Web.Commands.Cron
{
    public class UpsertTriggerCommandHandler : IRequestHandler<UpsertTriggerCommand>
    {
        private readonly IUnitOfWorkFactory factory;

        public UpsertTriggerCommandHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        async Task IRequestHandler<UpsertTriggerCommand>.Handle(UpsertTriggerCommand request, CancellationToken cancellationToken)
        {
            var timer = new Stopwatch();
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();
                var modified = request.Trigger;

                if (modified.Id != 0)
                {
                    timer.Restart();
                    var trigger = await triggerRepo.FindByAsync(x => x.Id == modified.Id);
                    timer.Stop();
                    Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "trigger", timer.ElapsedMilliseconds);

                    if (trigger != null)
                    {
                        trigger.Config = modified.Config;
                        trigger.CronExpression = modified.CronExpression;
                        trigger.Description = modified.Description;
                        trigger.ClassName = modified.ClassName;
                        trigger.IsEnabled = modified.IsEnabled;
                        trigger.SentEmailOnError = modified.SentEmailOnError;
                        trigger.ShowInSearch = modified.ShowInSearch;
                        triggerRepo.Update(trigger);
                    }
                }
                else
                {
                    timer.Restart();
                    var trigger = await triggerRepo.FindByAsync(x => x.TriggerKey == modified.Name && x.Group == modified.Group);
                    timer.Stop();
                    Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "trigger", timer.ElapsedMilliseconds);

                    if (trigger == null)
                    {
                        triggerRepo.Add(new JobTrigger
                        {
                            TriggerKey = modified.Name,
                            Group = modified.Group,
                            Config = modified.Config,
                            CronExpression = modified.CronExpression,
                            Description = modified.Description,
                            ClassName = modified.ClassName,
                            IsEnabled = modified.IsEnabled,
                            SentEmailOnError = modified.SentEmailOnError,
                            ShowInSearch = modified.ShowInSearch,
                        });
                    }
                    else
                    {
                        Log.Error($"Trigger {modified.Group}.{modified.Name} already exist");
                    }
                }

                 await uow.SaveChangesAsync();
            }
        }
    }
}

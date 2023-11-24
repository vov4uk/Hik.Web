using System.Diagnostics;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Hik.Web.Commands.Cron
{
    public class UpsertTriggerCommandHandler : IRequestHandler<UpsertTriggerCommand, int>
    {
        private readonly IUnitOfWorkFactory factory;

        public UpsertTriggerCommandHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        public async Task<int> Handle(UpsertTriggerCommand request, CancellationToken cancellationToken)
        {
            var timer = new Stopwatch();
            JobTrigger updated = null;
            JobTrigger newEntity = null;
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();
                if (request.Trigger.Id != 0)
                {
                    var modified = request.Trigger;
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

                    updated = trigger;
                }
                else
                {
                    var added = request.Trigger;
                    timer.Restart();
                    var trigger = await triggerRepo.FindByAsync(x => x.TriggerKey == added.Name && x.Group == added.Group);
                    timer.Stop();
                    Log.Information("Query: {type}; Method {method} Duration: {duration}ms;", this.GetType().Name, "trigger", timer.ElapsedMilliseconds);

                    if (trigger == null)
                    {
                        newEntity = triggerRepo.Add(new JobTrigger
                        {
                            TriggerKey = added.Name,
                            Group = added.Group,
                            Config = added.Config,
                            CronExpression = added.CronExpression,
                            Description = added.Description,
                            ClassName = added.ClassName,
                            IsEnabled = added.IsEnabled,
                            SentEmailOnError = added.SentEmailOnError,
                            ShowInSearch = added.ShowInSearch,
                        });

                    }
                    else
                    {
                        Log.Error($"Trigger {added.Group}.{added.Name} already exist");
                    }
                }
            }

            return updated?.Id ?? newEntity?.Id ?? 0;
        }
    }
}

using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Hik.Web.Commands.Cron
{
    public class RemoveTriggerCommandHandler : IRequestHandler<RemoveTriggerCommand>
    {
        private readonly IUnitOfWorkFactory factory;

        public RemoveTriggerCommandHandler(IUnitOfWorkFactory factory)
        {
            this.factory = factory;
        }

        public async Task Handle(RemoveTriggerCommand request, CancellationToken cancellationToken)
        {
            using (var uow = factory.CreateUnitOfWork(QueryTrackingBehavior.NoTracking))
            {
                var triggerRepo = uow.GetRepository<JobTrigger>();
                var trigger = await triggerRepo.FindByIdAsync(request.TriggerId);

                if (trigger != null)
                {
                    var dailyStatRepo = uow.GetRepository<DailyStatistic>();
                    var dailyStat = await dailyStatRepo.FindManyAsync(x => x.JobTriggerId == request.TriggerId);
                    if (dailyStat?.Count > 0)
                    {
                        dailyStatRepo.RemoveRange(dailyStat);
                    }

                    var filesRepo = uow.GetRepository<MediaFile>();
                    var files = await filesRepo.FindManyAsync(x => x.JobTriggerId == request.TriggerId);
                    if (files?.Count > 0)
                    {
                        filesRepo.RemoveRange(files);
                    }

                    var exRepo = uow.GetRepository<ExceptionLog>();
                    var ex = await exRepo.FindManyAsync(x => x.Job.JobTriggerId == request.TriggerId, x => x.Job);
                    if (ex?.Count > 0)
                    {
                        exRepo.RemoveRange(ex);
                    }

                    var jobsRepo = uow.GetRepository<HikJob>();
                    var jobs = await jobsRepo.FindManyAsync(x => x.JobTriggerId == request.TriggerId && x.Id != trigger.LastExecutedJobId);
                    if (jobs?.Count > 0)
                    {
                        jobsRepo.RemoveRange(jobs);
                    }

                    triggerRepo.Remove(trigger);

                    uow.SaveChanges();
                }
            }

        }
    }
}

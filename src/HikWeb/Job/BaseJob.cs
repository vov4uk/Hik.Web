using System;
using Autofac;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Infrastructure;
using Quartz;
using System.Linq;
using System.Threading.Tasks;
using HikConsole.DataAccess;
using HikConsole.DataAccess.Data;
using HikConsole.Scheduler;

namespace HikWeb.Job
{
    public abstract class BaseJob : IJob
    {
        protected static readonly ILogger Logger = Logger = AppBootstrapper.Container.Resolve<ILogger>();

        protected static readonly AppConfig Config = AppBootstrapper.Container.Resolve<IHikConfig>().Config;
        static BaseJob()
        {
            Logger.Info(Config.ToString());
        }

        public abstract Task<JobResult> InternalExecute(IJobExecutionContext context);

        public async Task Execute(IJobExecutionContext context)
        {
            var currentJobs = await context.Scheduler.GetCurrentlyExecutingJobs();

            if (currentJobs.Any(x => Equals(x.JobDetail.Key, context.JobDetail.Key) && x.FireInstanceId != context.FireInstanceId))
            {
                Logger.Info($"{context.JobDetail.Key} Already running. Skip!");
                return;
            }

            Logger.Info($"{context.JobDetail.Key} Execution started");

            var job = new HikJob
            {
                Started = DateTime.Now,
                JobType = context.JobDetail.Key.Name
            };

            using (var unitOfWork = new UnitOfWorkFactory().CreateUnitOfWork(Config.ConnectionString))
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                await jobRepo.Add(job);
                await unitOfWork.SaveChangesAsync();
            }

            var result = await this.InternalExecute(context);

            job.Finished = DateTime.Now;

            var jobResultSaver = new JobResultsSaver(Config.ConnectionString, job, result, Logger);
            await jobResultSaver.SaveAsync();

            Logger.Info($"{context.JobDetail.Key} Execution finished");
        }
    }
}

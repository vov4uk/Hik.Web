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
using HikConsole.DTO;
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

            var unitOfWorkFactory = new UnitOfWorkFactory(Config.ConnectionString);

            using (var unitOfWork = unitOfWorkFactory.CreateUnitOfWork())
            {
                var jobRepo = unitOfWork.GetRepository<HikJob>();
                await jobRepo.Add(job);
                await unitOfWork.SaveChangesAsync();
            }

            var result = await this.InternalExecute(context);

            job.Finished = DateTime.Now;
            Logger.Info("Save to DB...");
            var jobResultSaver = new JobService(unitOfWorkFactory, job, result);
            await jobResultSaver.SaveAsync();
            Logger.Info("Save to DB. Done");

            Logger.Info($"{context.JobDetail.Key} Execution finished");
        }
    }
}

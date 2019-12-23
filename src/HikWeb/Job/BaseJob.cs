using Autofac;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Infrastructure;
using Quartz;
using System.Linq;
using System.Threading.Tasks;

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

        public abstract Task InternalExecute(IJobExecutionContext context);

        public async Task Execute(IJobExecutionContext context)
        {
            var currentJobs = await context.Scheduler.GetCurrentlyExecutingJobs();

            if (currentJobs.Any(x => Equals(x.JobDetail.Key, context.JobDetail.Key) && x.FireInstanceId != context.FireInstanceId))
            {
                Logger.Info($"{context.JobDetail.Key} Already running. Skip!");
                return;
            }

            Logger.Info($"{context.JobDetail.Key} Execution started");
            await this.InternalExecute(context);
            Logger.Info($"{context.JobDetail.Key} Execution finished");
        }
    }
}

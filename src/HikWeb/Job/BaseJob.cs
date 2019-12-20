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
        protected static ILogger logger;
        protected static AppConfig appConfig;
        static BaseJob()
        {
            var container = AppBootstrapper.ConfigureIoc();
            appConfig = container.Resolve<IHikConfig>().Config;
            logger = container.Resolve<ILogger>();
            logger.Info(appConfig.ToString());            
        }

        public abstract Task InternalExecute(IJobExecutionContext context);

        public async Task Execute(IJobExecutionContext context)
        {
            var currentJobs = await context.Scheduler.GetCurrentlyExecutingJobs();

            if (currentJobs.Any(x => x.JobDetail.Key == context.JobDetail.Key && x.FireInstanceId != context.FireInstanceId))
            {
                logger.Info($"{context.JobDetail.Key} Already running. Skip!");
                return;
            }

            logger.Info($"{context.JobDetail.Key} Execution started");
            await this.InternalExecute(context);
            logger.Info($"{context.JobDetail.Key} Execution finished");
        }
    }
}

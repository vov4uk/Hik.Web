using Autofac;
using HikConsole.Config;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;
using Quartz;
using System.Threading.Tasks;

namespace HikWeb.Job
{
    public class HikDownloaderJob : BaseJob
    {
        static readonly HikDownloader Downloader = AppBootstrapper.Container.Resolve<HikDownloader>(new TypedParameter(typeof(AppConfig), Config));

        public override async Task InternalExecute(IJobExecutionContext context)
        {
            var result = await Downloader.DownloadAsync();
            var jobResultSaver = new JobResultsSaver(Config.ConnectionString, result, Logger);
            await jobResultSaver.SaveAsync();
        }
    }
}

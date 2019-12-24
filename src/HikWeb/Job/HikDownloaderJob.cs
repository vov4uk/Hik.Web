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

        public override async Task<JobResult> InternalExecute(IJobExecutionContext context)
        {
            JobResult result = await Downloader.DownloadAsync();
            return result;
        }
    }
}

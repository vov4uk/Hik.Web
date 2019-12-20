using Autofac;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;
using Quartz;
using System.Linq;
using System.Threading.Tasks;

namespace HikWeb.Job
{
    public class HikDownloaderJob : BaseJob
    {
        static HikDownloader downloader;
        static HikDownloaderJob()
        {
            var container = AppBootstrapper.ConfigureIoc();
            downloader = container.Resolve<HikDownloader>(new TypedParameter(typeof(AppConfig), appConfig));
        }

        public override async Task InternalExecute(IJobExecutionContext context)
        {
            var result = await downloader.DownloadAsync();
            var jobResultSaver = new JobResultsSaver(appConfig.ConnectionString, result, logger);
            await jobResultSaver.SaveAsync();
        }
    }
}

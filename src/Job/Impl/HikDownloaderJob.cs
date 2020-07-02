using System.Threading.Tasks;
using Autofac;
using HikConsole.DTO;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;

namespace Job.Impl
{
    public class HikDownloaderJob : JobProcessBase
    {
        public HikDownloaderJob(string description, string path, string connectionString) : base(description, path, connectionString)
        {
        }

        public override JobType JobType => JobType.HikDownloader;


        public async override Task<JobResult> Run()
        {
            var downloader = AppBootstrapper.Container.Resolve<HikDownloader>();
            return await downloader.DownloadAsync(this.ConfigPath);
        }
    }
}

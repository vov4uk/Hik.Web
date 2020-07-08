using System.Threading.Tasks;
using Autofac;
using HikConsole.DTO;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;
using Job.Email;

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
            downloader.ExceptionFired += Downloader_ExceptionFired;
            return await downloader.DownloadAsync(this.ConfigPath);
        }

        private void Downloader_ExceptionFired(object sender, HikConsole.Events.ExceptionEventArgs e)
        {
            Logger.Error(e.Exception.Message, e.Exception);
            EmailHelper.Send(e.Exception);
            System.Console.WriteLine(e.ToString());
        }
    }
}

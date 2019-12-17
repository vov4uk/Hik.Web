using System.Reactive.Concurrency;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.Helpers;
using HikConsole.Scheduler;
using Topshelf;

namespace HikConsole.Service
{
    public class ServiceStarter
    {
        public void StartService(AppConfig config, HikDownloader downloader, ILogger logger)
        {
            Guard.NotNull(() => config, config);

            HostFactory.Run(
                x =>
                {
                    x.Service<MonitoringInstance>(
                        s =>
                        {
                            s.ConstructUsing(name => new MonitoringInstance(TaskPoolScheduler.Default, config, downloader, new DeleteArchiving(), logger));
                            s.WhenStarted(tc => tc.Start());
                            s.WhenStopped(tc => tc.Stop());
                        });

                    x.RunAsLocalSystem();
                    x.SetDescription("Camera Monitoring Service");
                    x.SetDisplayName("HikConsole Service");
                    x.SetServiceName("HikConsole");
                });
        }
    }
}

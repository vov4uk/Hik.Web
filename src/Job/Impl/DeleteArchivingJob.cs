using System;
using System.Threading.Tasks;
using Autofac;
using HikConsole.DTO;
using HikConsole.Infrastructure;
using HikConsole.Scheduler;

namespace Job.Impl
{
    public class DeleteArchivingJob : JobProcessBase
    {
        public DeleteArchivingJob(string description, string path, string connectionString, Guid activityId) : base(description, path, connectionString, activityId)
        {
        }

        public override JobType JobType =>JobType.DeleteArchiving;

        public async override Task<JobResult> Run()
        {
            var worker = AppBootstrapper.Container.Resolve<DeleteArchiving>();
            return await worker.ExecuteAsync(this.ConfigPath);
        }
    }
}

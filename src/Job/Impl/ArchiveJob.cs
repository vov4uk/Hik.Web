using Autofac;
using Hik.Client.Infrastructure;
using Hik.Client.Service;
using Hik.DataAccess;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class ArchiveJob : JobProcessBase
    {
        public ArchiveJob(string trigger, string configFilePath, string connectionString, Guid activityId)
            : base(trigger, configFilePath, connectionString, activityId)
        {
            Config = HikConfig.GetConfig<ArchiveConfig>(configFilePath);
        }

        public override JobType JobType => JobType.Archive;

        public override Task InitializeProcessingPeriod()
        {
            return Task.CompletedTask;
        }

        public override async Task<IReadOnlyCollection<FileDTO>> Run()
        {
            var worker = AppBootstrapper.Container.Resolve<ArchiveService>();
            worker.ExceptionFired += base.ExceptionFired;

            return await worker.ExecuteAsync(Config, DateTime.MinValue, DateTime.MinValue);
        }

        public override Task SaveResults(IReadOnlyCollection<FileDTO> files, JobService service)
        {
            JobInstance.PeriodStart = files.Min(x => x.Date);
            JobInstance.PeriodEnd = files.Max(x => x.Date);
            return base.SaveResults(files, service);
        }
    }
}

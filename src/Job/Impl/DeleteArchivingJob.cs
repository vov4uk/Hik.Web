using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Hik.Client.Infrastructure;
using Hik.Client.Service;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Job.Impl
{
    public class DeleteArchivingJob : JobProcessBase
    {
        public DeleteArchivingJob(string description, string configFilePath, string connectionString, Guid activityId) 
            : base(description, configFilePath, connectionString, activityId)
        {
            Config = HikConfig.GetConfig<CameraConfig>(configFilePath);
        }

        public override JobType JobType => JobType.DeleteArchiving;

        public override Task InitializeProcessingPeriod()
        {
            var config = Config as CameraConfig;
            var period = TimeSpan.FromDays(config.RetentionPeriodDays.Value);
            DateTime cutOff = DateTime.Today.Subtract(period);
            JobInstance.PeriodEnd = cutOff;
            return Task.CompletedTask;
        }

        public async override Task<IReadOnlyCollection<FileDTO>> Run()
        {
            var worker = AppBootstrapper.Container.Resolve<DeleteSevice>();
            worker.ExceptionFired += base.ExceptionFired;

            return await worker.ExecuteAsync(Config, DateTime.MinValue, JobInstance.PeriodEnd.Value);
        }
    }
}

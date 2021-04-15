using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Hik.Client.Infrastructure;
using Hik.Client.Service;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Job.Impl
{
    public class DeleteArchivingJob : JobProcessBase
    {
        public DeleteArchivingJob(string trigger, string configFilePath, string connectionString, Guid activityId) 
            : base(trigger, configFilePath, connectionString, activityId)
        {
            Config = HikConfigExtentions.GetConfig<CameraConfig>(configFilePath);
            LogInfo(Config?.ToString());
        }

        public override Task InitializeProcessingPeriod()
        {
            var config = Config as CameraConfig;
            var period = TimeSpan.FromDays(config.RetentionPeriodDays.Value);
            DateTime cutOff = DateTime.Today.Subtract(period);
            JobInstance.PeriodEnd = cutOff;
            return Task.CompletedTask;
        }

        public override async Task SaveHistory(IReadOnlyCollection<MediaFile> files, JobService service)
        {
            await service.SaveHistoryFilesAsync<DeleteHistory>(files);
        }

        public async override Task<IReadOnlyCollection<MediaFileDTO>> Run()
        {
            var worker = AppBootstrapper.Container.Resolve<DeleteSevice>();
            worker.ExceptionFired += base.ExceptionFired;

            return await worker.ExecuteAsync(Config, DateTime.MinValue, JobInstance.PeriodEnd.Value);
        }
    }
}

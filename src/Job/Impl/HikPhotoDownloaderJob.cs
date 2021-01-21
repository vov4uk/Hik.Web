using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Hik.DTO.Contracts;
using Hik.Client.Infrastructure;
using Hik.Client.Service;
using Hik.DTO.Config;

namespace Job.Impl
{
    public class HikPhotoDownloaderJob : JobProcessBase
    {
        public HikPhotoDownloaderJob(string description, string configFilePath, string connectionString, Guid activityId) 
            : base(description, configFilePath, connectionString, activityId)
        {
            Config = HikConfig.GetConfig<CameraConfig>(configFilePath);
        }

        public override JobType JobType => JobType.HikPhotoDownloader;

        public async override Task<IReadOnlyCollection<MediaFileDTO>> Run()
        {
            var downloader = AppBootstrapper.Container.Resolve<HikPhotoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;

            return await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
        }
    }
}

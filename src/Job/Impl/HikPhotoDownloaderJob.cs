using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.Infrastructure;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class HikPhotoDownloaderJob : HikJobBase
    {
        public HikPhotoDownloaderJob(string trigger, string configFilePath, IJobService db, IEmailHelper email, Guid activityId)
            : base(trigger, db, email, activityId)
        {
            Config = HikConfigExtensions.GetConfig<CameraConfig>(configFilePath);
            LogInfo(Config?.ToString());
        }

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> RunAsync()
        {
            var downloader = AppBootstrapper.Container.Resolve<IHikPhotoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;

            CalculateProcessingPeriod();

            var files = await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
            downloader.ExceptionFired -= base.ExceptionFired;
            return files;
        }
    }
}
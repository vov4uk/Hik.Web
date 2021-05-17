using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Hik.DTO.Contracts;
using Hik.Client.Infrastructure;
using Hik.Client.Service;
using Hik.DTO.Config;
using Hik.DataAccess.Data;
using Hik.DataAccess;
using Job.Extensions;

namespace Job.Impl
{
    public class HikPhotoDownloaderJob : JobProcessBase
    {
        public HikPhotoDownloaderJob(string trigger, string configFilePath, string connectionString, Guid activityId) 
            : base(trigger, configFilePath, connectionString, activityId)
        {
            Config = HikConfigExtensions.GetConfig<CameraConfig>(configFilePath);
            LogInfo(Config?.ToString());
        }

        public override async Task SaveHistory(IReadOnlyCollection<MediaFile> files, JobService service)
        {
            await service.SaveHistoryFilesAsync<DownloadHistory>(files);
        }

        public async override Task<IReadOnlyCollection<MediaFileDTO>> Run()
        {
            var downloader = AppBootstrapper.Container.Resolve<HikPhotoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;

            return await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
        }
    }
}

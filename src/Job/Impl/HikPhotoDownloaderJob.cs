using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Hik.DTO.Contracts;
using HikConsole.Scheduler;
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

        public override async Task InitializeProcessingPeriod()
        {
            var config = Config as CameraConfig;
            DateTime jobStart = JobInstance.Started;

            var camera = await GetCamera(Config.Alias);
            DateTime? lastSync = camera?.LastPhotoSync;

            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * config.ProcessingPeriodHours);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;
        }

        public async override Task<IReadOnlyCollection<MediaFileBase>> Run()
        {
            var downloader = AppBootstrapper.Container.Resolve<HikPhotoDownloaderService>();
            downloader.ExceptionFired += base.ExceptionFired;

            return await downloader.ExecuteAsync(Config, this.JobInstance.PeriodStart.Value, this.JobInstance.PeriodEnd.Value);
        }

        public override async Task SaveResults(IReadOnlyCollection<MediaFileBase> files, JobService service)
        {
            JobInstance.PhotosCount += files.Count;

            var convertedFiles = files.OfType<FileDTO>().ToList();
            JobInstance.PhotosCount = convertedFiles.Count();
            await service.SavePhotosAsync(convertedFiles, Config);
        }
    }
}

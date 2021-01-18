using Autofac;
using Hik.Client.Infrastructure;
using Hik.Client.Service;
using Hik.DTO.Contracts;
using HikConsole.Scheduler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class CleanUpJob : JobProcessBase
    {
        public CleanUpJob(string trigger, string configFilePath, string connectionString, Guid activityId)
            : base(trigger, configFilePath, connectionString, activityId)
        {
        }

        public override JobType JobType => JobType.CleanUp;

        public override Task InitializeProcessingPeriod()
        {
            return Task.CompletedTask;
        }

        public override async Task<IReadOnlyCollection<MediaFileBase>> Run()
        {
            var worker = AppBootstrapper.Container.Resolve<CleanUpService>();
            worker.ExceptionFired += base.ExceptionFired;

            return await worker.ExecuteAsync(AppConfig.Camera, DateTime.MinValue, DateTime.MinValue);
        }

        public override async Task SaveResults(IReadOnlyCollection<MediaFileBase> files, JobService service)
        {
            var convertedFiles = files.OfType<DeletedFileDTO>().ToList();
            JobInstance.VideosCount = convertedFiles.Count(x => x.Extention == ".mp4");
            JobInstance.PhotosCount = convertedFiles.Count(x => x.Extention == ".jpg");
            await service.SaveDeletedFilesAsync(convertedFiles, AppConfig.Camera);
        }
    }
}

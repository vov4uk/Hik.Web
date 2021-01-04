using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Hik.Client.Infrastructure;
using Hik.Client.Scheduler;
using Hik.DTO.Contracts;
using HikConsole.Scheduler;

namespace Job.Impl
{
    public class DeleteArchivingJob : JobProcessBase
    {
        public DeleteArchivingJob(string description, string path, string connectionString, Guid activityId) 
            : base(description, path, connectionString, activityId)
        {
        }

        public override JobType JobType => JobType.DeleteArchiving;

        public override Task InitializeProcessingPeriod()
        {
            var period = TimeSpan.FromDays(AppConfig.Camera.RetentionPeriodDays.Value);
            DateTime cutOff = DateTime.Today.Subtract(period);
            JobInstance.PeriodEnd = cutOff;
            return Task.CompletedTask;
        }

        public async override Task<IReadOnlyCollection<MediaFileBase>> Run()
        {
            var worker = AppBootstrapper.Container.Resolve<DeleteArchiveSevice>();
            worker.ExceptionFired += base.ExceptionFired;

            return await worker.ExecuteAsync(AppConfig.Camera, DateTime.MinValue, JobInstance.PeriodEnd.Value);
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

using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.FileProviders;
using Hik.Client.Infrastructure;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class GarbageCollectorJob : JobProcessBase
    {
        protected readonly IDirectoryHelper directoryHelper;
        protected readonly IFilesHelper filesHelper;
        protected readonly IFileProvider filesProvider;

        public GarbageCollectorJob(string trigger, string configFilePath, IJobService db, IEmailHelper email, Guid activityId)
            : base(trigger, db, email, activityId)
        {
            Config = HikConfigExtensions.GetConfig<GarbageCollectorConfig>(configFilePath);
            LogInfo(Config.ToString());

            this.directoryHelper = AppBootstrapper.Container.Resolve<IDirectoryHelper>();
            this.filesHelper = AppBootstrapper.Container.Resolve<IFilesHelper>();
            this.filesProvider = AppBootstrapper.Container.Resolve<IFileProvider>();
        }

        protected override Task<IReadOnlyCollection<MediaFileDTO>> RunAsync()
        {
            var gcConfig = Config as GarbageCollectorConfig;

            IReadOnlyCollection<MediaFileDTO> deleteFilesResult;

            if (gcConfig.RetentionPeriodDays > 0)
            {
                var period = TimeSpan.FromDays(gcConfig.RetentionPeriodDays);
                var cutOff = DateTime.Today.Subtract(period);
                filesProvider.Initialize(new[] { gcConfig.DestinationFolder });
                deleteFilesResult = filesProvider.GetFilesOlderThan(gcConfig.FileExtention, cutOff);

                this.DeleteFiles(deleteFilesResult);
            }
            else
            {
                deleteFilesResult = PersentageDelete(gcConfig);
            }

            directoryHelper.DeleteEmptyDirs(gcConfig.DestinationFolder);
            return Task.FromResult(deleteFilesResult);
        }

        protected override async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDTO> files)
        {
            JobInstance.PeriodStart = files.Min(x => x.Date);
            JobInstance.PeriodEnd = files.Max(x => x.Date);
            JobInstance.FilesCount = files.Count;

            await db.UpdateDailyStatisticsAsync(JobInstance, files);

            var config = (GarbageCollectorConfig)Config;
            if (JobInstance.PeriodEnd.HasValue && JobInstance.FilesCount > 0 && config.Triggers?.Any() == true)
            {
                await db.DeleteObsoleteJobsAsync(config.Triggers, JobInstance.PeriodEnd.Value);
            }
        }

        private void DeleteFiles(IReadOnlyCollection<MediaFileDTO> filesToDelete)
        {
            foreach (var file in filesToDelete)
            {
                this.logger.Debug($"Deleting: {file.Path}");
#if RELEASE
                file.Size = filesHelper.FileSize(file.Path);
                this.filesHelper.DeleteFile(file.Path);
#endif
            }
        }
        private List<MediaFileDTO> PersentageDelete(GarbageCollectorConfig gcConfig)
        {
            List<MediaFileDTO> deletedFiles = new();
            var destination = gcConfig.DestinationFolder;
            var totalSpace = this.directoryHelper.GetTotalSpaceBytes(destination) * 1.0;
            do
            {
                var freeSpace = this.directoryHelper.GetTotalFreeSpaceBytes(destination) * 1.0;

                var freePercentage = 100 * freeSpace / totalSpace;
                this.logger.Info($"Destination: {destination} Free Percentage: {freePercentage,2} %");

                if (freePercentage < gcConfig.FreeSpacePercentage)
                {
                    filesProvider.Initialize(gcConfig.TopFolders);
                    var filesToDelete = filesProvider.GetNextBatch(gcConfig.FileExtention);
                    if (!filesToDelete.Any())
                    {
                        break;
                    }
                    this.DeleteFiles(filesToDelete);
                    deletedFiles.AddRange(filesToDelete);
                }
                else
                {
                    break;
                }
            }
            while (true);
            return deletedFiles;
        }
    }
}
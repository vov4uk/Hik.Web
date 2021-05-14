using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Job.Extensions;

namespace Job.Impl
{
    public class GarbageCollectorJob : JobProcessBase
    {
        private readonly IDirectoryHelper directoryHelper;
        private readonly IFilesHelper filesHelper;

        public GarbageCollectorJob(string trigger, string configFilePath, string connectionString, Guid activityId)
            : base(trigger, configFilePath, connectionString, activityId)
        {
            Config = HikConfigExtensions.GetConfig<GarbageCollectorConfig>(configFilePath);
            LogInfo(Config?.ToString());
            this.directoryHelper = new DirectoryHelper();
            this.filesHelper = new FilesHelper();
        }

        protected override Task InitializeProcessingPeriod()
        {
            return Task.CompletedTask;
        }

        protected override Task<IReadOnlyCollection<MediaFileDTO>> Run()
        {
            var cleanupConfig = Config as GarbageCollectorConfig;
            var destination = Config.DestinationFolder;

            List<MediaFileDTO> deleteFilesResult = new();
            var fileProvider = new FilesProvider();
            using var dbContext = new DataContext(this.ConnectionString);

            do
            {
                double totalSpace = this.directoryHelper.GetTotalSpaceGb(destination);
                double freeSpace = this.directoryHelper.GetTotalFreeSpaceGb(destination);

                var freePercentage = 100 * freeSpace / totalSpace;
                this.logger.Info($"Destination: {destination} Free Percentage: {freePercentage,2}");

                if (freePercentage < cleanupConfig.FreeSpacePercentage)
                {
                    fileProvider.Initialize(cleanupConfig.Triggers, dbContext);
                    IReadOnlyCollection<MediaFile> filesToDelete = fileProvider.GetNextBatch();

                    var deletedFiles = this.DeleteFiles(filesToDelete);
                    if (deletedFiles.Count <= 0)
                    {
                        break;
                    }

                    deleteFilesResult.AddRange(deletedFiles);
                }
                else
                {
                    break;
                }
            }
            while (true);

            directoryHelper.DeleteEmptyDirs(destination);
            return Task.FromResult((IReadOnlyCollection<MediaFileDTO>) deleteFilesResult);
        }

        private List<MediaFileDTO> DeleteFiles(IReadOnlyCollection<MediaFile> filesToDelete)
        {
            List<MediaFileDTO> result = new();
            foreach (var file in filesToDelete)
            {
                this.logger.Debug($"Deleting: {file.Path}");
#if RELEASE
                this.filesHelper.DeleteFile(file.Path);
#endif
                result.Add(new MediaFileDTO { Id = file.Id, Duration = file.Duration, Date = file.Date, Name = file.Name, Size = file.Size, Path = file.Path });
            }

            return result;
        }

        protected override async Task SaveResults(IReadOnlyCollection<MediaFileDTO> files, JobService service)
        {
            JobInstance.PeriodStart = files.Min(x => x.Date);
            JobInstance.PeriodEnd = files.Max(x => x.Date);
            JobInstance.FilesCount = files.Count;

            await service.UpdateDailyStatistics(files);
            await SaveHistory(files.Select(x => new MediaFile { Id = x.Id }).ToList(), service);
        }

        protected override Task SaveHistory(IReadOnlyCollection<MediaFile> files, JobService service)
        {
            return service.SaveHistoryFilesAsync<DeleteHistory>(files);
        }
    }
}

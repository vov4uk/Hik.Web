using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class DbMigrationJob : JobProcessBase
    {
        public DbMigrationJob(string trigger, string configFilePath, IUnitOfWorkFactory unitOfWorkFactory, Guid activityId)
            : base(HikConfigExtensions.GetConfig<MigrationConfig>(configFilePath).TriggerKey, unitOfWorkFactory, activityId)
        {
            Config = HikConfigExtensions.GetConfig<MigrationConfig>(configFilePath);
            LogInfo(trigger);
            LogInfo(Config?.ToString());
        }

        protected override void CalculateProcessingPeriod()
        {
            // Not applicable
        }

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> RunAsync()
        {
            var deleteHelper = new DeleteHelper(new DirectoryHelper(), new FilesHelper());
            deleteHelper.Initialize(Config.DestinationFolder);

            bool readDuration = ((MigrationConfig)Config).ReadDuration;

            List<MediaFileDTO> files = new();
            do
            {
                var batch = await deleteHelper.GetNextBatch(readDuration);
                if (batch.Count > 0)
                {
                    files.AddRange(batch);
                    LogInfo($"Files found {files.Count}");
                }
                else
                {
                    break;
                }
            } while (true);

            return files;
        }
        protected override Task SaveHistoryAsync(IReadOnlyCollection<MediaFile> files, JobService service)
        {
            return service.SaveHistoryFilesAsync<DownloadHistory>(files);
        }
    }
}
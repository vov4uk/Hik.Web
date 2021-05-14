using Hik.Client.Helpers;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Job.Extensions;

namespace Job.Impl
{
    public class DbMigrationJob : JobProcessBase
    {
        public DbMigrationJob(string trigger, string configFilePath, string connectionString, Guid activityId)
            : base(HikConfigExtensions.GetConfig<MigrationConfig>(configFilePath).TriggerKey, configFilePath, connectionString, activityId)
        {
            Config = HikConfigExtensions.GetConfig<MigrationConfig>(configFilePath);
            LogInfo(trigger);
            LogInfo(Config?.ToString());
        }

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> Run()
        {
            var deleteHelper = new DeleteHelper(new DirectoryHelper(), new FilesHelper());
            deleteHelper.Initialize(Config.DestinationFolder);

            List<MediaFileDTO> files = new();
            do
            {
                var batch = await deleteHelper.GetNextBatch(((MigrationConfig)Config).ReadDuration);
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

        protected override Task InitializeProcessingPeriod()
        {
            return Task.CompletedTask;
        }

        protected override Task SaveHistory(IReadOnlyCollection<MediaFile> files, JobService service)
        {
            return service.SaveHistoryFilesAsync<DownloadHistory>(files);
        }
    }
}

using Hik.Client.Helpers;
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
    public class DbMigrationJob : JobProcessBase
    {
        public DbMigrationJob(string trigger, string configFilePath, IJobService db, IEmailHelper email, Guid activityId)
            : base(HikConfigExtensions.GetConfig<MigrationConfig>(configFilePath).TriggerKey, db, email, activityId)
        {
            Config = HikConfigExtensions.GetConfig<MigrationConfig>(configFilePath);
            LogInfo(trigger);
            LogInfo(Config?.ToString());
        }

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> RunAsync()
        {
            var deleteHelper = new DeleteHelper(new DirectoryHelper(), new FilesHelper());
            deleteHelper.Initialize(Config.DestinationFolder);

            bool readVideoDuration = ((MigrationConfig)Config).ReadVideoDuration;

            List<MediaFileDTO> files = new();
            do
            {
                var batch = await deleteHelper.GetNextBatch(readVideoDuration);
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
    }
}
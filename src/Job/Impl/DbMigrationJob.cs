using Autofac;
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
    public class DbMigrationJob : JobProcessBase
    {
        protected readonly IFileProvider filesProvider;

        public DbMigrationJob(string trigger, string configFilePath, IHikDatabase db, IEmailHelper email, Guid activityId)
            : base(HikConfigExtensions.GetConfig<MigrationConfig>(configFilePath).TriggerKey, db, email, activityId)
        {
            Config = HikConfigExtensions.GetConfig<MigrationConfig>(configFilePath);
            LogInfo($"Trigger : {trigger}, Config :{Config}");
            this.filesProvider = AppBootstrapper.Container.Resolve<IFileProvider>();
        }

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> RunAsync()
        {
            filesProvider.Initialize(new[] { Config.DestinationFolder });

            bool readVideoDuration = ((MigrationConfig)Config).ReadVideoDuration;

            List<MediaFileDTO> files = new();
            do
            {
                var batch = await filesProvider.GetOldestFilesBatch(readVideoDuration);
                if (batch.Any())
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
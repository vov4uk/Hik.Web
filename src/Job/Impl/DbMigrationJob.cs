using Hik.Client.FileProviders;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using NLog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class DbMigrationJob : JobProcessBase<MigrationConfig>
    {
        protected readonly IFileProvider filesProvider;

        public DbMigrationJob(MigrationConfig config, IFileProvider fileProvider, IHikDatabase db, IEmailHelper email, ILogger logger)
            : base(config.TriggerKey, config, db, email, logger)
        {
            this.filesProvider = fileProvider;
        }

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> RunAsync()
        {
            filesProvider.Initialize(new[] { Config.DestinationFolder });

            bool readVideoDuration = Config.ReadVideoDuration;

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
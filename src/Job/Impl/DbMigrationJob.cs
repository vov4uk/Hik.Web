using CSharpFunctionalExtensions;
using Hik.Client.FileProviders;
using Hik.DataAccess.Abstractions;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Microsoft.Extensions.Logging;
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

        protected override async Task<Result<IReadOnlyCollection<MediaFileDto>>> RunAsync()
        {
            filesProvider.Initialize(new[] { Config.DestinationFolder });

            bool readVideoDuration = Config.ReadVideoDuration;

            List<MediaFileDto> files = new();
            do
            {
                var batch = await filesProvider.GetOldestFilesBatch(readVideoDuration);
                if (batch.Any())
                {
                    files.AddRange(batch);
                    logger.LogInformation("Files found {count}", files.Count);
                }
                else
                {
                    break;
                }
            } while (true);

            return Result.Success<IReadOnlyCollection<MediaFileDto>>(files);
        }
    }
}
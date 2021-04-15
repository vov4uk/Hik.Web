using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public class CleanUpService : DeleteJobBase
    {
        private const double Gb = 1024.0 * 1024.0 * 1024.0;

        public CleanUpService(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
            : base(directoryHelper, filesHelper)
        {
        }

        protected override Task<IReadOnlyCollection<MediaFileDTO>> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            var cleanupConfig = config as CleanupConfig;
            var destination = config.DestinationFolder;

            List<MediaFileDTO> deleteFilesResult = new List<MediaFileDTO>();
            var allFiles = this.directoryHelper.EnumerateFiles(destination).Select(x => KeyValuePair.Create(x, this.filesHelper.GetCreationDate(x))).OrderByDescending(file => file.Value);

            double freePercentage = 0.0;
            int page = 0;
            int pageSize = cleanupConfig.BatchSize;
            do
            {
                double totalSpace = this.directoryHelper.GetTotalSpace(destination) / Gb;
                double freeSpace = this.directoryHelper.GetTotalFreeSpace(destination) / Gb;

                freePercentage = 100 * freeSpace / totalSpace;
                this.logger.Info($"Destination: {destination} Free Percentage: {freePercentage,2}");

                if (freePercentage < cleanupConfig.FreeSpacePercentage)
                {
                    var filesToDelete = allFiles.SkipLast(page * pageSize).TakeLast(pageSize).ToList();

                    var deletedFiles = this.DeleteFiles(filesToDelete, destination);
                    if (deletedFiles.Count <= 0)
                    {
                        break;
                    }

                    deleteFilesResult.AddRange(deletedFiles);
                    page++;
                }
                else
                {
                    break;
                }
            }
            while (true);

            directoryHelper.DeleteEmptyDirs(destination);

            return Task.FromResult(deleteFilesResult as IReadOnlyCollection<MediaFileDTO>);
        }
    }
}

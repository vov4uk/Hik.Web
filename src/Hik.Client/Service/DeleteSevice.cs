using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public class DeleteSevice : DeleteJobBase
    {
        public DeleteSevice(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
            : base(directoryHelper, filesHelper)
        {
        }

        protected override Task<IReadOnlyCollection<MediaFileDTO>> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            logger.Info("Start DeleteSevice");
            string destination = config.DestinationFolder;
            List<KeyValuePair<string, DateTime>> filesList = this.directoryHelper.EnumerateFiles(destination)
                .Select(x => KeyValuePair.Create(x, this.filesHelper.GetCreationDate(x)))
                .Where(x => x.Value < to)
                .ToList();
            List<MediaFileDTO> result = DeleteFiles(filesList, destination);
            directoryHelper.DeleteEmptyDirs(destination);
            return Task.FromResult(result as IReadOnlyCollection<MediaFileDTO>);
        }
    }
}

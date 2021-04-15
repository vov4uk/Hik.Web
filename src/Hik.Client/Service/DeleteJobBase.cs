using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public abstract class DeleteJobBase : RecurrentJobBase<MediaFileDTO>
    {
        protected readonly IFilesHelper filesHelper;

        protected DeleteJobBase(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
            : base(directoryHelper)
        {
            this.filesHelper = filesHelper;
        }

        protected List<MediaFileDTO> DeleteFiles(List<KeyValuePair<string, DateTime>> filesToDelete, string basePath)
        {
            List<MediaFileDTO> result = new List<MediaFileDTO>();
            foreach (var file in filesToDelete)
            {
                this.logger.Debug($"Deleting: {file.Key}");
                if (filesHelper.FileExists(file.Key))
                {
                    var size = filesHelper.FileSize(file.Key);
                    this.filesHelper.DeleteFile(file.Key);
                    result.Add(new MediaFileDTO { Date = file.Value, Name = file.Key.Remove(0, basePath.Length), Size = size });
                }
            }

            return result;
        }

        protected abstract override Task<IReadOnlyCollection<MediaFileDTO>> RunAsync(BaseConfig config, DateTime from, DateTime to);
    }
}

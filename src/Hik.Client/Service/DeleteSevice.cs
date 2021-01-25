using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using NLog;

namespace Hik.Client.Service
{
    public class DeleteSevice : RecurrentJobBase<MediaFileDTO>
    {
        private readonly IDirectoryHelper directoryHelper;
        private readonly IFilesHelper filesHelper;

        public DeleteSevice(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
        {
            this.directoryHelper = directoryHelper;
            this.filesHelper = filesHelper;
        }

        public override async Task<IReadOnlyCollection<MediaFileDTO>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to)
        {
            logger.Info("Start DeleteSevice");

            var cameraResult = await DeleteInternal(to, config as CameraConfig);

            return cameraResult;
        }

        private async Task<IReadOnlyCollection<MediaFileDTO>> DeleteInternal(DateTime cutOff, CameraConfig camera)
        {
            var destination = camera.DestinationFolder;

            if (!directoryHelper.DirectoryExists(destination))
            {
                logger.Error($"Output doesn't exist: {destination}");
                return default;
            }

            var filesToDelete = directoryHelper.EnumerateFiles(destination);

            logger.Info($"Destination: {destination}");
            logger.Info($"Found: {filesToDelete.Count} files");

            return await Task.Run(() =>
            {
                try
                {
                    var result = DeleteFiles(filesToDelete, cutOff, destination);
                    directoryHelper.DeleteEmptyFolders(destination);
                    return result;
                }
                catch (Exception ex)
                {
                    OnExceptionFired(new ExceptionEventArgs(ex), camera);
                }

                return default;
            });
        }

        private List<MediaFileDTO> DeleteFiles(List<string> filesToDelete, DateTime cutOff, string basePath)
        {
            List<MediaFileDTO> result = new List<MediaFileDTO>();
            filesToDelete.ForEach(
                    file =>
                    {
                        var date = filesHelper.GetCreationDate(file);

                        if (date < cutOff)
                        {
                            logger.Debug($"Deleting: {file}");
                            var size = filesHelper.FileSize(file);
                            var dateCreated = filesHelper.GetCreationDate(file);
                            this.filesHelper.DeleteFile(file);
                            result.Add(new MediaFileDTO { Date = date, Name = file.Remove(0, basePath.Length), Size = size });
                        }
                    });
            return result;
        }
    }
}

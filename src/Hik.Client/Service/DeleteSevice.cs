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
    public class DeleteSevice : IRecurrentJob<DeletedFileDTO>
    {
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IDirectoryHelper directoryHelper;
        private readonly IFilesHelper filesHelper;

        public DeleteSevice(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
        {
            this.directoryHelper = directoryHelper;
            this.filesHelper = filesHelper;
        }

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public async Task<IReadOnlyCollection<DeletedFileDTO>> ExecuteAsync(CameraConfig config, DateTime from, DateTime to)
        {
            logger.Info("Start DeleteSevice");

            var cameraResult = await DeleteInternal(to, config);

            return cameraResult;
        }

        protected virtual void OnExceptionFired(ExceptionEventArgs e)
        {
            ExceptionFired?.Invoke(this, e);
        }

        private async Task<IReadOnlyCollection<DeletedFileDTO>> DeleteInternal(DateTime cutOff, CameraConfig camera)
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
            List<DeletedFileDTO> deleteFilesResult = new List<DeletedFileDTO>();

            return await Task.Run(() =>
            {
                try
                {
                    deleteFilesResult.AddRange(DeleteFiles(filesToDelete, cutOff));
                    directoryHelper.DeleteEmptyFolders(destination);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, ex.ToString());
                    ex.Data.Add("Camera", camera.Alias);
                    OnExceptionFired(new ExceptionEventArgs(ex));
                }

                return deleteFilesResult;
            });
        }

        private List<DeletedFileDTO> DeleteFiles(List<string> filesToDelete, DateTime cutOff)
        {
            List<DeletedFileDTO> deletedFiles = new List<DeletedFileDTO>();
            filesToDelete.ForEach(
                    file =>
                    {
                        var date = filesHelper.GetCreationDate(file);

                        if (date < cutOff)
                        {
                            logger.Debug($"Deleting: {file}");
                            filesHelper.DeleteFile(file);
                            deletedFiles.Add(new DeletedFileDTO(filesHelper.GetFileNameWithoutExtension(file), filesHelper.GetExtension(file)));
                        }
                    });
            return deletedFiles;
        }
    }
}

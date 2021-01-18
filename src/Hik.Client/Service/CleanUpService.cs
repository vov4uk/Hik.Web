using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using NLog;

namespace Hik.Client.Service
{
    public class CleanUpService : IRecurrentJob<DeletedFileDTO>
    {
        private const double Gb = 1024.0 * 1024.0 * 1024.0;
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IDirectoryHelper directoryHelper;
        private readonly IFilesHelper filesHelper;

        public CleanUpService(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
        {
            this.directoryHelper = directoryHelper;
            this.filesHelper = filesHelper;
        }

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public Task<IReadOnlyCollection<DeletedFileDTO>> ExecuteAsync(CameraConfig config, DateTime from, DateTime to)
        {
            var destination = config.DestinationFolder;
            this.logger.Info("Start CleanUpService");
            if (!this.directoryHelper.DirectoryExists(destination))
            {
                this.logger.Error($"Output doesn't exist: {destination}");
                return default;
            }

            var allFiles = this.directoryHelper.EnumerateFiles(destination).OrderByDescending(file => this.filesHelper.GetCreationDate(file));
            List<DeletedFileDTO> deleteFilesResult = new List<DeletedFileDTO>();

            this.logger.Info($"Destination: {destination}");
            this.logger.Info($"Found: {allFiles.Count()} files");

            double freePercentage = 0.0;
            int page = 0;
            int pageSize = 100;
            do
            {
                var totalSpace = this.directoryHelper.GetTotalSpace(destination) / Gb;
                var freeSpace = this.directoryHelper.GetTotalFreeSpace(destination) / Gb;

                freePercentage = 100 * freeSpace / totalSpace;
                this.logger.Info($"Free Percentage: {freePercentage,2}");

                if (freePercentage < 10.0)
                {
                    var filesToDelete = allFiles.SkipLast(page * pageSize).TakeLast(pageSize).ToList();

                    try
                    {
                        var deletedFiles = this.DeleteFiles(filesToDelete);
                        if (deletedFiles.Count > 0)
                        {
                            deleteFilesResult.AddRange(deletedFiles);
                            page++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(ex, ex.ToString());
                        this.OnExceptionFired(new ExceptionEventArgs(ex));
                    }
                }
                else
                {
                    break;
                }
            }
            while (true);

            this.directoryHelper.DeleteEmptyFolders(destination);

            return Task.FromResult(deleteFilesResult as IReadOnlyCollection<DeletedFileDTO>);
        }

        protected virtual void OnExceptionFired(ExceptionEventArgs e)
        {
            this.ExceptionFired?.Invoke(this, e);
        }

        private List<DeletedFileDTO> DeleteFiles(List<string> filesToDelete)
        {
            List<DeletedFileDTO> deletedFiles = new List<DeletedFileDTO>();
            filesToDelete.ForEach(
                    file =>
                    {
                        this.logger.Debug($"Deleting: {file}");
                        if (filesHelper.FileExists(file))
                        {
                            this.filesHelper.DeleteFile(file);
                            deletedFiles.Add(new DeletedFileDTO(this.filesHelper.GetFileNameWithoutExtension(file), this.filesHelper.GetExtension(file)));
                        }
                    });
            return deletedFiles;
        }
    }
}

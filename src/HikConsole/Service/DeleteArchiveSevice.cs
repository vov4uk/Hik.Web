using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HikConsole.Abstraction;
using HikConsole.DTO.Config;
using HikConsole.DTO.Contracts;
using HikConsole.Events;
using NLog;

namespace HikConsole.Scheduler
{
    public class DeleteArchiveSevice : IRecurrentJob<DeletedFileDTO>
    {
        private const string AllFilter = "*";
        private const string DateFormat = "yyyyMMdd";
        private readonly string[] filesToDelete = new[] { ".mp4", ".jpg", ".ini" };
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public DeleteArchiveSevice()
        {
        }

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public async Task<IReadOnlyCollection<DeletedFileDTO>> ExecuteAsync(CameraConfig config, DateTime from, DateTime to)
        {
            this.logger.Info("Start.");

            var cameraResult = await this.ArchiveInternal(to, config, this.filesToDelete);

            return cameraResult;
        }

        protected virtual void OnExceptionFired(ExceptionEventArgs e)
        {
            this.ExceptionFired?.Invoke(this, e);
        }

        private async Task<IReadOnlyCollection<DeletedFileDTO>> ArchiveInternal(DateTime cutOff, CameraConfig camera, string[] extensions)
        {
            var destination = camera.DestinationFolder;

            if (!Directory.Exists(destination))
            {
                this.logger.Warn($"Output doesn't exist: {destination}");
                return default;
            }

            var filesToDelete = Directory.EnumerateFiles(destination, AllFilter, SearchOption.AllDirectories)
                    .Where(s => extensions.Any(ext => ext == Path.GetExtension(s)))
                    .ToList();

            this.logger.Info($"Destination: {destination}");
            this.logger.Info($"Found: {filesToDelete.Count} files");
            List<DeletedFileDTO> deleteFilesResult = new List<DeletedFileDTO>();

            return await Task.Run(() =>
            {
                try
                {
                    deleteFilesResult.AddRange(this.DeleteFiles(filesToDelete, cutOff));
                    this.DeleteEmptyFolders(destination);
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, ex.ToString());
                    ex.Data.Add("Camera", camera.Alias);
                    this.OnExceptionFired(new ExceptionEventArgs(ex));
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
                        var fileName = Path.GetFileName(file);
                        if (!DateTime.TryParseExact(fileName.Substring(0, 8), DateFormat, null, System.Globalization.DateTimeStyles.None, out var date))
                        {
                            var fileInfo = new FileInfo(file);
                            date = fileInfo.CreationTime;
                        }

                        if (date < cutOff)
                        {
                            this.logger.Debug($"Deleting: {file}");
                            File.Delete(file);
                            deletedFiles.Add(new DeletedFileDTO(fileName, Path.GetExtension(file)));
                        }
                    });
            return deletedFiles;
        }

        private void DeleteEmptyFolders(string destination)
        {
            var directories = Directory.EnumerateDirectories(destination, AllFilter, SearchOption.AllDirectories);
            foreach (var directory in directories)
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    this.logger.Info($"Deleting: {directory}");
                    Directory.Delete(directory);
                }
            }
        }
    }
}

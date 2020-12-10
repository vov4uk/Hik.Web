using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.DTO;
using HikConsole.DTO.Contracts;
using NLog;

namespace HikConsole.Scheduler
{
    public class DeleteArchiving : IRecurrentJob
    {
        private const string AllFilter = "*";
        private const string DateFormat = "yyyyMMdd";
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IHikConfig hikConfig;
        private readonly IMapper mapper;

        public DeleteArchiving(
            IHikConfig hikConfig,
            IMapper mapper)
        {
            this.hikConfig = hikConfig;
            this.mapper = mapper;
        }

        public async Task<JobResult> ExecuteAsync(string configFilePath)
        {
            var appConfig = this.hikConfig.GetConfig(configFilePath);

            this.logger.Info("Start.");

            var jobResult = new JobResult();

            foreach (var cameraConf in appConfig.Cameras)
            {
                var period = TimeSpan.FromDays(cameraConf.RetentionPeriodDays.Value);
                DateTime cutOff = DateTime.Today.Subtract(period);

                var cameraResult = await this.ArchiveInternal(cutOff, cameraConf, appConfig.FilesToDelete);
                jobResult.StoreCameraResult(cameraConf.Alias, cameraResult);
            }

            return jobResult;
        }

        private Task<CameraResult> ArchiveInternal(DateTime cutOff, CameraConfig camera, string[] extensions)
        {
            var destination = camera.DestinationFolder;

            if (!Directory.Exists(destination))
            {
                this.logger.Warn($"Output doesn't exist: {destination}");
                return default;
            }

            var result = new CameraResult(this.mapper.Map<CameraDTO>(camera));
            var filesToDelete = Directory.EnumerateFiles(destination, AllFilter, SearchOption.AllDirectories)
                    .Where(s => extensions.Any(ext => ext == Path.GetExtension(s)))
                    .ToList();

            this.logger.Info($"Destination: {destination}");
            this.logger.Info($"Found: {filesToDelete.Count} files");

            return Task.Run(() =>
            {
                try
                {
                    var deleteFilesResult = this.DeleteFiles(filesToDelete, cutOff);
                    this.DeleteEmptyFolders(destination);
                    result.DeletedFiles.AddRange(deleteFilesResult);
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, ex.ToString());
                    result.Failed = true;
                }

                return result;
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
                    this.logger.Debug($"Deleting: {directory}");
                    Directory.Delete(directory);
                }
            }
        }
    }
}

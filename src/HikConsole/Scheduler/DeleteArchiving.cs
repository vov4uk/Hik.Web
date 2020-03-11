using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.DTO;
using HikConsole.DTO.Contracts;

namespace HikConsole.Scheduler
{
    public class DeleteArchiving : IDeleteArchiving
    {
        private readonly ILogger logger;

        public DeleteArchiving(ILogger log)
        {
            this.logger = log;
        }

        public Task<JobResult> Archive(CameraConfig[] cameras, TimeSpan time, string[] extentions)
        {
            return Task.Run(() =>
            {
                this.logger.Info("Start.");
                DateTime cutOff = DateTime.Today.Subtract(time);

                var jobResult = new JobResult
                {
                    PeriodStart = default,
                    PeriodEnd = cutOff,
                };

                foreach (var cameraConf in cameras)
                {
                    var cameraResult = this.ArchiveInternal(cameraConf.DestinationFolder, cutOff, cameraConf, extentions);
                    jobResult.CameraResults.Add(cameraConf.Alias, cameraResult);
                }

                return jobResult;
            });
        }

        private CameraResult ArchiveInternal(string destination, DateTime cutOff, CameraConfig camera, string[] extentions)
        {
            if (!Directory.Exists(destination))
            {
                this.logger.Warn($"Output doesn't exist: {destination}");
                return default;
            }

            var result = this.CreateCameraResult(camera);

            List<string> files = new List<string>();
            foreach (var ext in extentions)
            {
                var filesToDelete = Directory.EnumerateFiles(destination, ext, SearchOption.AllDirectories);
                files.AddRange(filesToDelete);
            }

            this.logger.Info($"Destination: {destination}");
            this.logger.Info($"Found: {files.Count} files");

            files.ForEach(
                file =>
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        if (!DateTime.TryParseExact(fileName.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
                        {
                            var fileInfo = new FileInfo(file);
                            date = fileInfo.CreationTime;
                        }

                        if (date < cutOff)
                        {
                            this.logger.Debug($"Deleting: {file}");
                            File.Delete(file);
                            result.DeletedFiles.Add(new DeletedFileDTO() { FilePath = fileName, Extention = Path.GetExtension(file) });
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(ex.ToString(), ex);
                        result.Failed = true;
                    }
                });

            try
            {
                var directories = Directory.EnumerateDirectories(destination, "*", SearchOption.AllDirectories);
                foreach (var directory in directories)
                {
                    if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        this.logger.Debug($"Deleting: {directory}");
                        Directory.Delete(directory);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error("Error hapened", ex);
                result.Failed = true;
            }

            return result;
        }

        private CameraResult CreateCameraResult(CameraConfig cameraConf)
        {
            return new CameraResult(new CameraDTO
            {
                Alias = cameraConf.Alias,
                DestinationFolder = cameraConf.DestinationFolder,
                IpAddress = cameraConf.IpAddress,
                PortNumber = cameraConf.PortNumber,
                UserName = cameraConf.UserName,
            });
        }
    }
}

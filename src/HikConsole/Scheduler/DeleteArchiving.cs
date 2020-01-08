using System;
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

        public Task<JobResult> Archive(CameraConfig[] cameras, TimeSpan time)
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
                    var cameraResult = new CameraResult(new CameraDTO
                    {
                        Alias = cameraConf.Alias,
                        DestinationFolder = cameraConf.DestinationFolder,
                        IpAddress = cameraConf.IpAddress,
                        PortNumber = cameraConf.PortNumber,
                        UserName = cameraConf.UserName,
                    });
                    jobResult.CameraResults.Add(cameraConf.Alias, cameraResult);

                    var failCount = this.ArchiveInternal(cameraConf.DestinationFolder, cutOff, cameraResult);
                    cameraResult.Failed = failCount > 0;
                }

                return jobResult;
            });
        }

        private int ArchiveInternal(string destination, DateTime cutOff, CameraResult camResult)
        {
            int failCount = 0;
            if (!Directory.Exists(destination))
            {
                this.logger.Warn($"Output doesn't exist: {destination}");
                return 0;
            }

            var files = Directory.EnumerateFiles(destination, "*.mp4", SearchOption.AllDirectories).ToList();
            files.AddRange(Directory.EnumerateFiles(destination, "*.jpg", SearchOption.AllDirectories).ToList());
            this.logger.Info($"Destination: {destination}");
            this.logger.Info($"Found: {files.Count} files");

            Parallel.ForEach(
                files,
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
                            camResult.DeletedFiles.Add(new DeletedFileDTO() { FilePath = fileName, Extention = Path.GetExtension(file) });
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(ex.ToString(), ex);
                        failCount++;
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
                failCount++;
            }

            return failCount;
        }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HikConsole.Abstraction;
using HikConsole.Config;
using HikConsole.DataAccess.Data;

namespace HikConsole.Scheduler
{
    public class DeleteArchiving : IDeleteArchiving
    {
        private readonly ILogger logger;

        public DeleteArchiving(ILogger log)
        {
            this.logger = log;
        }

        public JobResult Archive(CameraConfig[] cameras, TimeSpan time)
        {
            DateTime appStart = DateTime.Now;

            this.logger.Info($"Start.");
            DateTime cutOff = DateTime.Today.Subtract(time);

            var job = new Job { PeriodStart = default, PeriodEnd = cutOff, Started = appStart, JobType = nameof(DeleteArchiving) };
            var jobResult = new JobResult(job);

            foreach (var x in cameras)
            {
                var failCount = this.ArchiveInternal(x.DestinationFolder, cutOff);
                job.FailsCount += failCount;
            }

            job.Finished = DateTime.Now;
            return jobResult;
        }

        private int ArchiveInternal(string destination, DateTime cutOff)
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
            this.logger.Info($"Found: {files.Count()} files");

            Parallel.ForEach(
                files,
                file =>
                {
                    try
                    {
                        var fileName = Path.GetFileName(file);
                        DateTime date;
                        if (!DateTime.TryParseExact(fileName.Substring(0, 8), "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out date))
                        {
                            var fileInfo = new FileInfo(file);
                            date = fileInfo.CreationTime;
                        }

                        if (date < cutOff)
                        {
                            this.logger.Debug($"Deleting: {file}");
                            File.Delete(file);
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

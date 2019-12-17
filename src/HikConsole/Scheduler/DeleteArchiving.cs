using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HikConsole.Abstraction;
using NLog;

namespace HikConsole.Scheduler
{
    public class DeleteArchiving : IDeleteArchiving
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Task Archive(string destination, TimeSpan time)
        {
            return Task.Run(() => this.ArchiveInternal(destination, time));
        }

        private void ArchiveInternal(string destination, TimeSpan time)
        {
            if (!Directory.Exists(destination))
            {
                Log.Warn("Output doesn't exist: {0}", destination);
                return;
            }

            var files = Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories);
            DateTime cutOff = DateTime.Today.Subtract(time);
            Parallel.ForEach(
                files,
                file =>
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.CreationTime < cutOff)
                        {
                            Log.Debug("Deleting: {0}", file);
                            File.Delete(file);
                        }
                        else
                        {
                            Log.Debug("Keeping: {0}", file);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex);
                    }
                });

            try
            {
                var directories = Directory.EnumerateDirectories(destination);
                foreach (var directory in directories)
                {
                    if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    {
                        Directory.Delete(directory);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}

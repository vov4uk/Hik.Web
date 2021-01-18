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
    public class ArchiveService : IRecurrentJob<FileDTO>
    {
        // 2021 01 13 12 17 14 361
        private const string FileNameDateTimeFormat = "yyyyMMddHHmmssfff";
        private readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private readonly IDirectoryHelper directoryHelper;
        private readonly IFilesHelper filesHelper;

        public ArchiveService(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
        {
            this.directoryHelper = directoryHelper;
            this.filesHelper = filesHelper;
        }

        public event EventHandler<ExceptionEventArgs> ExceptionFired;

        public Task<IReadOnlyCollection<FileDTO>> ExecuteAsync(CameraConfig config, DateTime from, DateTime to)
        {
            var source = "c:\\FTP";
            var destination = config.DestinationFolder;

            this.logger.Info("Start ArchiveService");
            if (!this.directoryHelper.DirectoryExists(source))
            {
                this.logger.Error($"Output doesn't exist: {source}");
                return default;
            }

            var result = new List<FileDTO>();
            var allFiles = this.directoryHelper.EnumerateFiles(source);

            try
            {
                foreach (var oldFile in allFiles)
                {
                    var dateString = this.filesHelper.GetFileNameWithoutExtension(oldFile).Split('_')[2];
                    var ext = filesHelper.GetExtension(oldFile);
                    DateTime date;
                    if (!DateTime.TryParseExact(
                        dateString,
                        FileNameDateTimeFormat,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out date))
                    {
                        date = filesHelper.GetCreationDate(oldFile);
                    }

                    var newFilename = dateString + ext;
                    var workingDirectory = GetWorkingDirectory(destination, date);
                    var newFilePath = GetPathSafety(newFilename, workingDirectory);

                    this.filesHelper.RenameFile(oldFile, newFilePath);
                    result.Add(new FileDTO { Date = date, Name = newFilename, Duration = 1, Size = filesHelper.FileSize(newFilePath) });
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, ex.ToString());
                this.OnExceptionFired(new ExceptionEventArgs(ex));
            }

            return Task.FromResult(result as IReadOnlyCollection<FileDTO>);
        }

        protected virtual void OnExceptionFired(ExceptionEventArgs e)
        {
            this.ExceptionFired?.Invoke(this, e);
        }

        private string GetWorkingDirectory(string destinationFolder, DateTime date)
        {
            return filesHelper.CombinePath(destinationFolder, ToDirectoryNameString(date));
        }

        private string GetPathSafety(string file, string directory)
        {
            filesHelper.FolderCreateIfNotExist(directory);

            string destinationFilePath = filesHelper.CombinePath(directory, file);
            return destinationFilePath;
        }

        private string ToDirectoryNameString(DateTime date)
        {
            return $"{date.Year:0000}-{date.Month:00}\\{date.Day:00}\\{date.Hour:00}";
        }
    }
}

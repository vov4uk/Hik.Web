using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Events;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;

namespace Hik.Client.Service
{
    public class ArchiveService : RecurrentJobBase<MediaFileDTO>
    {
        // 2021 01 13 12 17 14 361
        private const string FileNameDateTimeFormat = "yyyyMMddHHmmssfff";
        private readonly IDirectoryHelper directoryHelper;
        private readonly IFilesHelper filesHelper;

        public ArchiveService(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
        {
            this.directoryHelper = directoryHelper;
            this.filesHelper = filesHelper;
        }

        public override Task<IReadOnlyCollection<MediaFileDTO>> ExecuteAsync(BaseConfig config, DateTime from, DateTime to)
        {
            var archiveConfig = config as ArchiveConfig;
            var source = archiveConfig.SourceFolder;
            var destination = config.DestinationFolder;

            this.logger.Info("Start ArchiveService");
            if (!this.directoryHelper.DirectoryExists(source))
            {
                this.logger.Error($"Output doesn't exist: {source}");
                return default;
            }

            var result = new List<MediaFileDTO>();
            var allFiles = this.directoryHelper.EnumerateFiles(source);

            try
            {
                foreach (var oldFile in allFiles)
                {
                    var dateString = this.filesHelper.GetFileNameWithoutExtension(oldFile).Split('_')[2];
                    var ext = filesHelper.GetExtension(oldFile);

                    if (!DateTime.TryParseExact(
                        dateString,
                        FileNameDateTimeFormat,
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var date))
                    {
                        date = filesHelper.GetCreationDate(oldFile);
                    }

                    var newFilename = dateString + ext;
                    var workingDirectory = GetWorkingDirectory(destination, date);
                    var newFilePath = GetPathSafety(newFilename, workingDirectory);

                    this.filesHelper.RenameFile(oldFile, newFilePath);
                    result.Add(new MediaFileDTO { Date = date, Name = newFilename, Duration = 1, Size = filesHelper.FileSize(newFilePath) });
                }
            }
            catch (Exception ex)
            {
                this.OnExceptionFired(new ExceptionEventArgs(ex), config);
            }

            return Task.FromResult(result as IReadOnlyCollection<MediaFileDTO>);
        }

        private string GetWorkingDirectory(string destinationFolder, DateTime date)
        {
            return filesHelper.CombinePath(destinationFolder, date.ToPhotoDirectoryNameString());
        }

        private string GetPathSafety(string file, string directory)
        {
            filesHelper.FolderCreateIfNotExist(directory);

            string destinationFilePath = filesHelper.CombinePath(directory, file);
            return destinationFilePath;
        }
    }
}

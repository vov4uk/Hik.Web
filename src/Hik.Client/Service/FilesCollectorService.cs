using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hik.Client.Abstraction.Services;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Newtonsoft.Json;
using Serilog;

namespace Hik.Client.Service
{
    public class FilesCollectorService : RecurrentJobBase, IFilesCollectorService
    {
        private readonly IFilesHelper filesHelper;
        private readonly IVideoHelper videoHelper;
        private Regex regex;

        public FilesCollectorService(
            IDirectoryHelper directoryHelper,
            IFilesHelper filesHelper,
            IVideoHelper videoHelper,
            ILogger logger)
            : base(directoryHelper, logger)
        {
            this.filesHelper = filesHelper;
            this.videoHelper = videoHelper;
        }

        protected override async Task<IReadOnlyCollection<MediaFileDto>> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            var fConfig = config as FilesCollectorConfig;

            if (fConfig == null)
            {
                throw new ArgumentException("Invalid config");
            }

            this.regex = GetRegex(fConfig.FileNamePattern);

            var result = new List<MediaFileDto>();
            var allFiles = this.directoryHelper.EnumerateFiles(fConfig.SourceFolder, fConfig.AllowedFileExtentions.Split(";"))
                .SkipLast(fConfig.SkipLast);

            foreach (var filePath in allFiles)
            {
                DateTime date = GetCreationDate(fConfig.FileNameDateTimeFormat, filePath);
                int duration = await this.videoHelper.GetDuration(filePath);

                string fileExt = filesHelper.GetExtension(filePath);
                string newFileName = date.ToArchiveFileString(duration, fileExt);

                string newFilePath = MoveFile(fConfig.DestinationFolder, filePath, date, newFileName);

                var dto = new MediaFileDto
                {
                    Date = date,
                    Name = filesHelper.GetFileName(newFilePath),
                    Path = newFilePath,
                    Duration = duration,
                    Size = filesHelper.FileSize(newFilePath),
                };
                result.Add(dto);

                logger.Debug($"{filePath} -> {JsonConvert.SerializeObject(dto)}");
            }

            return result.AsReadOnly();
        }

        private static Regex GetRegex(string template)
        {
            // Handels regex special characters.
            template = Regex.Replace(template, @"[\\\^\$\.\|\?\*\+\(\)]", m => @"\" + m.Value, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)", RegexOptions.None, TimeSpan.FromMilliseconds(100)) + "$";
            return new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }

        private string MoveFile(string destinationFolder, string oldFilePath, DateTime date, string newFileName)
        {
            string newFilePath = GetPathSafety(newFileName, GetWorkingDirectory(destinationFolder, date));
            this.filesHelper.RenameFile(oldFilePath, newFilePath);
            return newFilePath;
        }

        private DateTime GetCreationDate(string dateTimeFormat, string oldFile)
        {
            string fileName = filesHelper.GetFileNameWithoutExtension(oldFile);
            List<string> nameParts = ReverseStringFormat(fileName);
            DateTime date = default;
            bool nameParsed = false;
            if (nameParts != null && nameParts.Count != 0)
            {
                foreach (var name in nameParts)
                {
                    if (DateTime.TryParseExact(
                    name,
                    dateTimeFormat,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out date))
                    {
                        nameParsed = true;
                        break;
                    }
                }
            }

            if (!nameParsed || date.Year == 1970 || date.Year == 1)
            {
                date = filesHelper.GetCreationDate(oldFile);
            }

            return date;
        }

        private string GetWorkingDirectory(string destinationFolder, DateTime date)
        {
            return filesHelper.CombinePath(destinationFolder, date.ToDirectoryName());
        }

        private string GetPathSafety(string file, string directory)
        {
            directoryHelper.CreateDirIfNotExist(directory);
            return filesHelper.CombinePath(directory, file);
        }

        private List<string> ReverseStringFormat(string str)
        {
            return this.regex.Match(str).Groups.Values.Select(x => x.Value).ToList();
        }
    }
}

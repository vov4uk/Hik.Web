using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    public class ArchiveService : RecurrentJobBase, IArchiveService
    {
        private readonly IFilesHelper filesHelper;
        private readonly IVideoHelper videoHelper;
        private readonly IImageHelper imageHelper;
        private Regex regex;

        public ArchiveService(
            IDirectoryHelper directoryHelper,
            IFilesHelper filesHelper,
            IVideoHelper videoHelper,
            IImageHelper imageHelper,
            ILogger logger)
            : base(directoryHelper, logger)
        {
            this.filesHelper = filesHelper;
            this.videoHelper = videoHelper;
            this.imageHelper = imageHelper;
        }

        protected override async Task<IReadOnlyCollection<MediaFileDto>> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            var aConfig = config as ArchiveConfig;

            if (aConfig == null)
            {
                throw new ArgumentException("Invalid config");
            }

            if (aConfig.UnzipFiles)
            {
                var allZips = this.directoryHelper.EnumerateFiles(aConfig.SourceFolder, new[] { ".zip" }).SkipLast(aConfig.SkipLast);
                foreach (var zip in allZips)
                {
                    try
                    {
                        this.filesHelper.UnZipFile(zip);
                    }
                    catch (IOException e)
                    {
                        this.logger.Error("ErrorMsg: {errorMsg}; Trace: {trace}", $"Failed to unzip file - {zip}", e.ToStringDemystified());
                    }
                    catch (InvalidDataException e)
                    {
                        this.logger.Error("ErrorMsg: {errorMsg}; Trace: {trace}", $"Invalid zip file - {zip}", e.ToStringDemystified());
                    }

                    try
                    {
                        this.filesHelper.DeleteFile(zip);
                    }
                    catch (IOException e)
                    {
                        this.logger.Error("ErrorMsg: {errorMsg}; Trace: {trace}", $"Failed to delete file - {zip}", e.ToStringDemystified());
                    }
                }
            }

            this.regex = this.GetRegex(aConfig.FileNamePattern);

            var result = new List<MediaFileDto>();
            var allFiles = this.directoryHelper.EnumerateFiles(aConfig.SourceFolder, aConfig.AllowedFileExtentions).SkipLast(aConfig.UnzipFiles ? 0 : aConfig.SkipLast);

            foreach (var filePath in allFiles)
            {
                DateTime date = GetCreationDate(aConfig.FileNameDateTimeFormat, filePath);
                int duration = await this.videoHelper.GetDuration(filePath);

                string fileExt = filesHelper.GetExtension(filePath);
                string newFileName = date.ToArchiveFileString(duration, fileExt);

                string desciption = imageHelper.GetDescriptionData(filePath);

                string newFilePath = MoveFile(aConfig.DestinationFolder, filePath, date, newFileName);

                var dto = new MediaFileDto
                {
                    Date = date,
                    Name = filesHelper.GetFileName(newFilePath),
                    Path = newFilePath,
                    Duration = duration,
                    Size = filesHelper.FileSize(newFilePath),
                    Objects = desciption,
                };
                result.Add(dto);

                logger.Debug($"{filePath} -> {JsonConvert.SerializeObject(dto)}");
            }

            return result.AsReadOnly();
        }

        private string MoveFile(string destinationFolder, string oldFilePath, DateTime date, string newFileName)
        {
            string newFilePath = GetPathSafety(newFileName, GetWorkingDirectory(destinationFolder, date));
            this.filesHelper.RenameFile(oldFilePath, newFilePath);
            this.imageHelper.SetDate(newFilePath, date);
            return newFilePath;
        }

        private DateTime GetCreationDate(string dateTimeFormat, string oldFile)
        {
            string fileName = filesHelper.GetFileNameWithoutExtension(oldFile);
            List<string> nameParts = ReverseStringFormat(fileName);
            DateTime date = default;
            bool nameParsed = false;
            if (nameParts != null && nameParts.Any())
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
            return this.regex.Match(str).Groups.Select(x => x.Value).ToList();
        }

        private Regex GetRegex(string template)
        {
            // Handels regex special characters.
            template = Regex.Replace(template, @"[\\\^\$\.\|\?\*\+\(\)]", m => @"\" + m.Value, RegexOptions.None, TimeSpan.FromMilliseconds(100));
            string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)", RegexOptions.None, TimeSpan.FromMilliseconds(100)) + "$";
            return new Regex(pattern, RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }
    }
}

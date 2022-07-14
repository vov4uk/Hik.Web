using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Hik.Helpers.Abstraction;
using Microsoft.Extensions.Logging;

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
            this.regex = this.GetRegex(aConfig.FileNamePattern);

            var result = new List<MediaFileDto>();
            var allFiles = this.directoryHelper.EnumerateFiles(aConfig.SourceFolder, aConfig.AllowedFileExtentions).SkipLast(aConfig.SkipLast);

            try
            {
                foreach (var filePath in allFiles)
                {
                    DateTime date = GetCreationDate(aConfig.FileNameDateTimeFormat, filePath);
                    int duration = await this.videoHelper.GetDuration(filePath);

                    string fileExt = filesHelper.GetExtension(filePath);
                    string newFileName = date.ToArchiveFileString(duration, fileExt);
                    string newFilePath = MoveFile(aConfig.DestinationFolder, filePath, date, newFileName);

                    result.Add(new MediaFileDto
                    {
                        Date = date,
                        Name = filesHelper.GetFileName(newFilePath),
                        Path = newFilePath,
                        Duration = duration,
                        Size = filesHelper.FileSize(newFilePath),
                    });
                }
            }
            catch (Exception ex)
            {
                this.OnExceptionFired(ex);
            }

            return result.AsReadOnly();
        }

        private string MoveFile(string destinationFolder, string oldFilePath, DateTime date, string newFileName)
        {
            string newFilePath = GetPathSafety(newFileName, GetWorkingDirectory(destinationFolder, date));
            if (IsPicture(oldFilePath))
            {
                this.imageHelper.SetDate(oldFilePath, newFilePath, date);
                this.filesHelper.DeleteFile(oldFilePath);
            }
            else
            {
                this.filesHelper.RenameFile(oldFilePath, newFilePath);
            }

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
            return filesHelper.CombinePath(destinationFolder, date.ToPhotoDirectoryNameString());
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
            template = Regex.Replace(template, @"[\\\^\$\.\|\?\*\+\(\)]", m => @"\" + m.Value);
            string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)") + "$";
            return new Regex(pattern);
        }

        private bool IsPicture(string filePath)
        {
            return filesHelper.GetExtension(filePath) == ".jpg";
        }
    }
}

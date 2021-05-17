using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        private readonly IFilesHelper filesHelper;
        private readonly IVideoHelper videoHelper;

        public ArchiveService(IDirectoryHelper directoryHelper, IFilesHelper filesHelper, IVideoHelper videoHelper)
            : base(directoryHelper)
        {
            this.filesHelper = filesHelper;
            this.videoHelper = videoHelper;
        }

        protected override Task<IReadOnlyCollection<MediaFileDTO>> RunAsync(BaseConfig config, DateTime from, DateTime to)
        {
            var archiveConfig = config as ArchiveConfig;

            var result = new List<MediaFileDTO>();
            var allFiles = this.directoryHelper.EnumerateFiles(archiveConfig.SourceFolder).SkipLast(archiveConfig.SkipLast);

            try
            {
                foreach (var oldFile in allFiles)
                {
                    DateTime date = GetCreationDate(archiveConfig.FileNameDateTimeFormat, oldFile, archiveConfig.FileNamePattern);
                    int duration = this.videoHelper.GetDuration(oldFile);

                    var newFileName = date.ToArchiveFileString(duration, filesHelper.GetExtension(oldFile));
                    var newFilePath = GetPathSafety(newFileName, GetWorkingDirectory(config.DestinationFolder, date));
                    this.filesHelper.RenameFile(oldFile, newFilePath);
                    result.Add(new MediaFileDTO { Date = date, Name = newFileName, Path = newFilePath, Duration = duration, Size = filesHelper.FileSize(newFilePath) });
                }
            }
            catch (Exception ex)
            {
                this.OnExceptionFired(new ExceptionEventArgs(ex), config);
            }

            return Task.FromResult(result as IReadOnlyCollection<MediaFileDTO>);
        }

        private DateTime GetCreationDate(string dateTimeFormat, string oldFile, string nameTemplate)
        {
            string fileName = filesHelper.GetFileNameWithoutExtension(oldFile);
            List<string> nameParts = ReverseStringFormat(nameTemplate, fileName);
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

        private List<string> ReverseStringFormat(string template, string str)
        {
            // Handels regex special characters.
            template = Regex.Replace(template, @"[\\\^\$\.\|\?\*\+\(\)]", m => "\\"
             + m.Value);

            string pattern = "^" + Regex.Replace(template, @"\{[0-9]+\}", "(.*?)") + "$";

            Regex r = new Regex(pattern);
            Match m = r.Match(str);

            List<string> ret = new List<string>();

            for (int i = 1; i < m.Groups.Count; i++)
            {
                ret.Add(m.Groups[i].Value);
            }

            return ret;
        }
    }
}

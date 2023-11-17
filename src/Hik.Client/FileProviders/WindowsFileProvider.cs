using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hik.DTO.Contracts;
using Hik.Helpers;
using Hik.Helpers.Abstraction;
using Serilog;

namespace Hik.Client.FileProviders
{
    public class WindowsFileProvider : IFileProvider
    {
        protected readonly ILogger logger;

        private const string DatePattern = "\\d+\\-\\d+\\\\\\d+\\\\\\d+";
        private static readonly string[] Extentions = new[] { ".*" };
        private readonly IFilesHelper filesHelper;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IVideoHelper videoHelper;
        private Stack<DateTime> dates;
        private Dictionary<DateTime, IList<string>> folders;
        private bool isInitialized = false;

        public WindowsFileProvider(
            IFilesHelper filesHelper,
            IDirectoryHelper directoryHelper,
            IVideoHelper videoHelper,
            ILogger logger)
        {
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
            this.videoHelper = videoHelper;
            this.logger = logger;
        }

        public bool IsInitialized => isInitialized;

        public void Initialize(string[] directories)
        {
            if (!isInitialized)
            {
                logger.Information("Initialize!");
                folders = new Dictionary<DateTime, IList<string>>();
                foreach (var topDirectory in directories)
                {
                    List<string> subFolders = directoryHelper.EnumerateAllDirectories(topDirectory);
                    foreach (var directory in subFolders.Where(x => Regex.IsMatch(x, DatePattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(10))))
                    {
                        var trim = directory.Remove(0, Math.Max(0, directory.Length - 13))
                            .Replace("\\", "-")
                            .Split("-")
                            .Select(int.Parse)
                            .ToArray();

                        folders.SafeAdd(new DateTime(trim[0], trim[1], trim[2], trim[3], 0, 0, DateTimeKind.Local), directory);
                    }

                    folders.SafeAdd(DateTime.Today, topDirectory);
                }

                dates = new Stack<DateTime>(folders.Keys.OrderByDescending(x => x).ToList());
                logger.Information($"{dates.Count} dates found");
            }

            isInitialized = true;
        }

        public IReadOnlyCollection<MediaFileDto> GetNextBatch(string fileExtention, int batchSize = 100)
        {
            var result = new List<MediaFileDto>();
            if (!isInitialized)
            {
                logger.Information("GetNextBatch !isInitialized");
                return result;
            }

            while (result.Count <= batchSize)
            {
                if (dates.Count > 0 && dates.TryPop(out var lastDate))
                {
                    result.AddRange(GetFiles(fileExtention, lastDate));
                }
                else
                {
                    break;
                }
            }

            logger.Information($"GetNextBatch result {result.Count}");
            return result;
        }

        public async Task<IReadOnlyCollection<MediaFileDto>> GetOldestFilesBatch(bool readDuration = false)
        {
            var files = new List<MediaFileDto>();
            if (isInitialized && dates.TryPop(out var last) && folders.TryGetValue(last, out IList<string> value))
            {
                foreach (var dir in value)
                {
                    var localFiles = directoryHelper.EnumerateFiles(dir, Extentions);
                    foreach (var file in localFiles)
                    {
                        var size = filesHelper.FileSize(file);
                        var duration = readDuration ? await videoHelper.GetDuration(file) : 0;
                        files.Add(
                            new MediaFileDto
                            {
                                Date = last,
                                Name = filesHelper.GetFileName(file),
                                Path = file,
                                Size = size,
                                Duration = duration,
                            });
                    }
                }
            }

            return files.AsReadOnly();
        }

        public IReadOnlyCollection<MediaFileDto> GetFilesOlderThan(string fileExtention, DateTime date)
        {
            var result = new List<MediaFileDto>();
            if (isInitialized && folders.Count > 0)
            {
                foreach (var fileDateTime in folders.Keys.Where(x => x <= date && folders.ContainsKey(x)))
                {
                    result.AddRange(GetFiles(fileExtention, fileDateTime));
                }
            }

            return result;
        }

        private List<MediaFileDto> GetFiles(string fileExtention, DateTime lastDate)
        {
            List<string> files = new ();
            if (folders.TryGetValue(lastDate, out IList<string> value))
            {
                foreach (var folder in value)
                {
                    if (!string.IsNullOrEmpty(fileExtention))
                    {
                        files.AddRange(directoryHelper.EnumerateFiles(folder, new[] { fileExtention }));
                    }
                    else
                    {
                        files.AddRange(directoryHelper.EnumerateFiles(folder));
                    }
                }
            }

            return files.ConvertAll(x => new MediaFileDto { Path = x, Date = lastDate });
        }
    }
}

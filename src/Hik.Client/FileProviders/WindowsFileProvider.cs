using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.Client.Helpers;
using Hik.DTO.Contracts;
using NLog;

namespace Hik.Client.FileProviders
{
    public class WindowsFileProvider : IFileProvider
    {
        protected readonly Logger logger = LogManager.GetCurrentClassLogger();
        private readonly IFilesHelper filesHelper;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IVideoHelper videoHelper;
        private Stack<DateTime> dates;
        private Dictionary<DateTime, IList<string>> folders;
        private bool isInitialized = false;

        public WindowsFileProvider(IFilesHelper filesHelper, IDirectoryHelper directoryHelper, IVideoHelper videoHelper)
        {
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
            this.videoHelper = videoHelper;
        }

        public bool IsInitialized => isInitialized;

        public void Initialize(string[] directories)
        {
            if (!isInitialized)
            {
                logger.Info("Initialize!");
                folders = new Dictionary<DateTime, IList<string>>();
                foreach (var topDirectory in directories)
                {
                    List<string> subFolders = directoryHelper.EnumerateAllDirectories(topDirectory);
                    foreach (var directory in subFolders)
                    {
                        var trim = directory.Remove(0, Math.Max(0, directory.Length - 13)).Replace("\\", "-").Split("-");

                        if (trim.Length == 4 && DateTime.TryParse($"{trim[0]}-{trim[1]}-{trim[2]} {trim[3]}:00:00", out var dt))
                        {
                            folders.SafeAdd(dt, directory);
                        }
                    }

                    folders.SafeAdd(DateTime.Today, topDirectory);
                }

                dates = new Stack<DateTime>(folders.Keys.OrderByDescending(x => x).ToList());
                logger.Info($"{dates.Count} dates found");
            }

            isInitialized = true;
        }

        public IReadOnlyCollection<MediaFileDTO> GetNextBatch(string fileExtention, int batchSize = 100)
        {
            var result = new List<MediaFileDTO>();
            if (!isInitialized)
            {
                logger.Info("GetNextBatch !isInitialized");
                return result;
            }

            logger.Info("GetNextBatch");
            while (result.Count <= batchSize)
            {
                if (dates.Any() && dates.TryPop(out var lastDate))
                {
                    if (folders.ContainsKey(lastDate))
                    {
                        List<string> files = new List<string>();
                        foreach (var folder in folders[lastDate])
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

                        result.AddRange(files.Select(x => new MediaFileDTO { Path = x, Date = lastDate }));
                    }
                }
                else
                {
                    break;
                }
            }

            logger.Info($"GetNextBatch result {result.Count}");
            return result;
        }

        public async Task<IReadOnlyCollection<MediaFileDTO>> GetOldestFilesBatch(bool readDuration = false)
        {
            var files = new List<MediaFileDTO>();
            if (isInitialized && dates.TryPop(out var last) && folders.ContainsKey(last))
            {
                foreach (var dir in folders[last])
                {
                    var localFiles = directoryHelper.EnumerateFiles(dir, new[] { ".*" });
                    foreach (var file in localFiles)
                    {
                        var size = filesHelper.FileSize(file);
                        var duration = readDuration ? await videoHelper.GetDuration(file) : 0;
                        files.Add(new MediaFileDTO { Date = last, Name = filesHelper.GetFileName(file), Path = file, Size = size, Duration = duration });
                    }
                }
            }

            return files.AsReadOnly();
        }

        public IReadOnlyCollection<MediaFileDTO> GetFilesOlderThan(string fileExtention, DateTime date)
        {
            var result = new List<MediaFileDTO>();
            if (isInitialized && folders.Any())
            {
                foreach (var fileDateTime in folders.Keys.Where(x => x <= date && folders.ContainsKey(x)))
                {
                    foreach (var folder in folders[fileDateTime])
                    {
                        result.AddRange(directoryHelper.EnumerateFiles(folder, new[] { fileExtention })
                            .Select(x => new MediaFileDTO { Path = x, Date = fileDateTime }));
                    }
                }
            }

            return result;
        }
    }
}

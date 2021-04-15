using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Hik.Client.Abstraction;
using Hik.DTO.Contracts;

namespace Hik.Client.Helpers
{
    public class DeleteHelper
    {
        private readonly IFilesHelper filesHelper;
        private readonly IDirectoryHelper directoryHelper;
        private readonly IVideoHelper videoHelper;
        private bool isInitialized = false;
        private Dictionary<DateTime, List<string>> directories;
        private Stack<DateTime> dates;

        public DeleteHelper(IDirectoryHelper directoryHelper, IFilesHelper filesHelper)
        {
            this.filesHelper = filesHelper;
            this.directoryHelper = directoryHelper;
            videoHelper = new VideoHelper();
        }

        public void Initialize(string destination)
        {
            if (!isInitialized)
            {
                directories = new Dictionary<DateTime, List<string>>();
                var subFolders = this.directoryHelper.EnumerateAllDirectories(destination);
                foreach (var folder in subFolders)
                {
                    var fol = folder[Math.Max(0, folder.Length - 13) ..].Replace(@"\", "-");
                    if (DateTime.TryParseExact(fol, "yyyy-MM-dd-HH", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var dt))
                    {
                        Console.WriteLine($"{dt} - {folder}");
                        if (directories.ContainsKey(dt))
                        {
                            directories[dt].Add(folder);
                        }
                        else
                        {
                            directories.Add(dt, new List<string> { folder });
                        }
                    }
                }

                dates = new Stack<DateTime>(directories.Keys.OrderBy(x => x).ToList());
            }
        }

        public Task<IReadOnlyCollection<MediaFileDTO>> GetNextBatch(bool readDuration = false)
        {
            var files = new List<MediaFileDTO>();
            if (dates.TryPop(out var last) && directories.ContainsKey(last))
            {
                foreach (var dir in directories[last])
                {
                    var localFiles = directoryHelper.EnumerateFiles(dir);
                    foreach (var file in localFiles)
                    {
                        var size = filesHelper.FileSize(file);
                        var duration = readDuration ? videoHelper.GetDuration(file) : 0;
                        files.Add(new MediaFileDTO { Date = last, Name = filesHelper.GetFileName(file), Path = file, Size = size, Duration = duration });
                    }
                }
            }

            return Task.FromResult(files as IReadOnlyCollection<MediaFileDTO>);
        }
    }
}

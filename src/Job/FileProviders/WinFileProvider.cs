using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Job.FileProviders
{
    public class WinFileProvider : IFileProvider
    {
        private Stack<DateTime> dates;
        Dictionary<DateTime, List<string>> folders;
        private bool isInitialized = false;
        public void Initialize(string[] trigger)
        {
            if (!isInitialized)
            {
                folders = new Dictionary<DateTime, List<string>>();
                foreach (var tr in trigger)
                {
                    foreach (var item in Directory.EnumerateDirectories(tr, "*.*", SearchOption.AllDirectories).OrderBy(x => x))
                    {
                        var trim = item.Remove(0, Math.Max(0, item.Length - 13)).Replace("\\", "-").Split("-");

                        if (trim.Length == 4)
                        {
                            if (DateTime.TryParse($"{trim[0]}-{trim[1]}-{trim[2]} {trim[3]}:00:00", out var dt))
                            {
                                if (folders.ContainsKey(dt))
                                {
                                    folders[dt].Add(item);
                                }
                                else
                                {
                                    folders.Add(dt, new List<string> { item });
                                }
                            }
                        }
                    }
                }
                dates = new Stack<DateTime>(folders.Keys.OrderByDescending(x => x).ToList());
            }
            isInitialized = true;
        }

        public IReadOnlyCollection<MediaFileDTO> GetNextBatch(int batchSize = 100)
        {
            var result = new List<MediaFileDTO>();
            if (dates.Any())
            {
                while (result.Count <= batchSize)
                {
                    if (dates.TryPop(out var lastDate))
                    {
                        if (folders.ContainsKey(lastDate))
                        {
                            foreach (var folder in folders[lastDate])
                            {
                                result.AddRange(Directory.EnumerateFiles(folder, "*.*").Select(x => new MediaFileDTO { Path = x, Date = lastDate}));
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        public IReadOnlyCollection<MediaFileDTO> GetFilesOlderThan(DateTime date)
        {
            var result = new List<MediaFileDTO>();
            if (folders.Any())
            {
                foreach (var fileDateTime in folders.Keys.Where(x => x <= date))
                {
                    if (folders.ContainsKey(fileDateTime))
                    {
                        foreach (var folder in folders[fileDateTime])
                        {
                            result.AddRange(Directory.EnumerateFiles(folder, "*.*").Select(x => new MediaFileDTO { Path = x, Date = fileDateTime }));
                        }
                    }
                }
            }

            return result;
        }
    }
}

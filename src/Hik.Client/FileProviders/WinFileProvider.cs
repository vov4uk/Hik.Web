﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hik.Client.FileProviders;
using Hik.Client.Helpers;
using Hik.DTO.Contracts;

namespace Job.FileProviders
{
    public class WinFileProvider : IFileProvider
    {
        private Stack<DateTime> dates;
        private Dictionary<DateTime, IList<string>> directoriesToDelete;
        private bool isInitialized = false;

        public void Initialize(string[] directories)
        {
            if (!isInitialized)
            {
                directoriesToDelete = new Dictionary<DateTime, IList<string>>();
                foreach (var topDirectory in directories)
                {
                    foreach (var directory in Directory.EnumerateDirectories(topDirectory, "*.*", SearchOption.AllDirectories).OrderBy(x => x))
                    {
                        var trim = directory.Remove(0, Math.Max(0, directory.Length - 13)).Replace("\\", "-").Split("-");

                        if (trim.Length == 4 && DateTime.TryParse($"{trim[0]}-{trim[1]}-{trim[2]} {trim[3]}:00:00", out var dt))
                        {
                            directoriesToDelete.SafeAdd(dt, directory);
                        }
                    }

                    directoriesToDelete.SafeAdd(DateTime.Today, topDirectory);
                }

                dates = new Stack<DateTime>(directoriesToDelete.Keys.OrderByDescending(x => x).ToList());
            }

            isInitialized = true;
        }

        public IReadOnlyCollection<MediaFileDTO> GetNextBatch(string fileExtention, int batchSize = 100)
        {
            var result = new List<MediaFileDTO>();

            while (result.Count <= batchSize)
            {
                if (dates.Any() && dates.TryPop(out var lastDate))
                {
                    if (directoriesToDelete.ContainsKey(lastDate))
                    {
                        foreach (var folder in directoriesToDelete[lastDate])
                        {
                            result
                                .AddRange(Directory.EnumerateFiles(folder, fileExtention)
                                .Select(x => new MediaFileDTO { Path = x, Date = lastDate }));
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            return result;
        }

        public IReadOnlyCollection<MediaFileDTO> GetFilesOlderThan(string fileExtention, DateTime date)
        {
            var result = new List<MediaFileDTO>();
            if (directoriesToDelete.Any())
            {
                foreach (var fileDateTime in directoriesToDelete.Keys.Where(x => x <= date && directoriesToDelete.ContainsKey(x)))
                {
                    foreach (var folder in directoriesToDelete[fileDateTime])
                    {
                        result.AddRange(Directory.EnumerateFiles(folder, fileExtention).Select(x => new MediaFileDTO { Path = x, Date = fileDateTime }));
                    }
                }
            }

            return result;
        }
    }
}
using Hik.DataAccess.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Hik.DataAccess
{
    public class FilesProvider
    {
        private bool isInitialized = false;
        private Dictionary<DateTime, List<MediaFile>> mediaFiles;
        private Stack<DateTime> dates;


        public void Initialize(string[] trigger, DataContext dbContenxt)
        {
            if (!isInitialized)
            {
                var triggers = dbContenxt.JobTriggers.Where(x => trigger.Contains(x.TriggerKey)).ToList();

                mediaFiles = new Dictionary<DateTime, List<MediaFile>>();

                foreach (var tri in triggers)
                {
                    var files = dbContenxt.MediaFiles
                        .Include(x => x.DeleteHistory)
                        .Where(x => x.JobTriggerId == tri.Id && x.DeleteHistory == null)
                        .OrderBy(x => x.Date);

                    foreach (var file in files)
                    {

                        var dt = file.Date;
                        if (mediaFiles.ContainsKey(dt))
                        {
                            mediaFiles[dt].Add(file);
                        }
                        else
                        {
                            mediaFiles.Add(dt, new List<MediaFile> { file });
                        }
                    }
                }

                dates = new Stack<DateTime>(mediaFiles.Keys.OrderByDescending(x => x).ToList());
                isInitialized = true;
            }
        }

        public IReadOnlyCollection<MediaFile> GetNextBatch(int batchSize = 100)
        {
            var result = new List<MediaFile>();
            if (dates.Any())
            {
                while (result.Count <= batchSize)
                {
                    if (dates.TryPop(out var last) && mediaFiles.ContainsKey(last))
                    {
                        result.AddRange(mediaFiles[last]);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }
        
        public IReadOnlyCollection<MediaFile> GetBatch(DateTime to)
        {
            var result = new List<MediaFile>();
            if (dates.Any())
            {
                var last = dates.Peek();
                while (true)
                {
                    if (last <= to && mediaFiles.ContainsKey(last))
                    {
                        result.AddRange(mediaFiles[last]);
                        dates.Pop();
                        last = dates.Peek();
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }
    }
}

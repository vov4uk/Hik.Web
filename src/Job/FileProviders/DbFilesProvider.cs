using AutoMapper;
using Hik.DataAccess;
using Hik.DTO.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Job.FileProviders
{
    public class DbFilesProvider : IFileProvider
    {
        private static readonly IMapper mapper = new MapperConfiguration(configureAutoMapper).CreateMapper();
        private bool isInitialized = false;
        private Dictionary<DateTime, List<MediaFileDTO>> mediaFiles;
        private Stack<DateTime> dates;
        DataContext dbContenxt;

        public DbFilesProvider(DataContext dbContenxt)
        {
            this.dbContenxt = dbContenxt;
        }

        public void Initialize(string[] trigger)
        {
            if (!isInitialized)
            {
                var triggers = dbContenxt.JobTriggers.Where(x => trigger.Contains(x.TriggerKey)).ToList();

                mediaFiles = new Dictionary<DateTime, List<MediaFileDTO>>();

                foreach (var tri in triggers)
                {
                    var files = dbContenxt.MediaFiles
                        .Where(x => x.JobTriggerId == tri.Id)
                        .OrderBy(x => x.Date);

                    foreach (var file in files)
                    {
                        var dt = file.Date;
                        if (mediaFiles.ContainsKey(dt))
                        {
                            mediaFiles[dt].Add(mapper.Map<MediaFileDTO>(file));
                        }
                        else
                        {
                            mediaFiles.Add(dt, new List<MediaFileDTO> { mapper.Map<MediaFileDTO>(file) });
                        }
                    }
                }

                dates = new Stack<DateTime>(mediaFiles.Keys.OrderByDescending(x => x).ToList());
                isInitialized = true;
            }
        }

        public IReadOnlyCollection<MediaFileDTO> GetNextBatch(int batchSize = 100)
        {
            var result = new List<MediaFileDTO>();
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

        public IReadOnlyCollection<MediaFileDTO> GetBatch(DateTime to)
        {
            var result = new List<MediaFileDTO>();
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

        static void configureAutoMapper(IMapperConfigurationExpression x)
        {
            x.AddProfile<AutoMapperProfile>();
        }
    }
}

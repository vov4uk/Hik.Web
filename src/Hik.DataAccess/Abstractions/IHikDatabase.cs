using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hik.DataAccess.Abstractions
{
    public interface IHikDatabase
    {
        Task<HikJob> CreateJobInstanceAsync(HikJob job);

        Task SaveJobResultAsync(HikJob job);

        Task UpdateJobAsync(HikJob job);

        Task LogExceptionToAsync(int jobId, string message);

        Task<JobTrigger> GetOrCreateJobTriggerAsync(string triggerKey);

        Task<List<MediaFile>> SaveFilesAsync(HikJob job, IReadOnlyCollection<MediaFileDto> files);

        Task UpdateDailyStatisticsAsync(int jobTriggerId, IReadOnlyCollection<MediaFileDto> files);

        Task SaveDownloadHistoryFilesAsync(HikJob job, IReadOnlyCollection<MediaFile> files);

        Task DeleteObsoleteJobsAsync(string[] triggers, DateTime to);
    }
}

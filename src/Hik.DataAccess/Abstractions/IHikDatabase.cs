using Hik.DataAccess.Data;
using Hik.DTO.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hik.DataAccess.Abstractions
{
    public interface IHikDatabase
    {
        void UpdateJobTrigger(JobTrigger trigger);

        HikJob CreateJob(HikJob job);

        void UpdateJob(HikJob job);

        void LogExceptionTo(int jobId, string message);

        Task<JobTrigger> GetJobTriggerAsync(string group, string key);

        Task<JobTrigger[]> GetJobTriggersAsync(int[] triggerIds);

        List<MediaFile> SaveFiles(HikJob job, IReadOnlyCollection<MediaFileDto> files);

        void SaveFile(HikJob job, MediaFileDto item);

        Task UpdateDailyStatisticsAsync(int jobTriggerId, IReadOnlyCollection<MediaFileDto> files);

        Task DeleteObsoleteJobsAsync(int[] triggers, DateTime to);
    }
}

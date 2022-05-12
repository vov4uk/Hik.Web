﻿using Hik.DataAccess.Data;
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

        Task LogExceptionToAsync(int jobId, string message, string callStack, uint? errorCode = null);

        Task<JobTrigger> GetOrCreateJobTriggerAsync(string triggerKey);

        Task<List<MediaFile>> SaveFilesAsync(HikJob job, IReadOnlyCollection<MediaFileDTO> files);

        Task UpdateDailyStatisticsAsync(int jobTriggerId, IReadOnlyCollection<MediaFileDTO> files);

        Task SaveDownloadHistoryFilesAsync(HikJob job, IReadOnlyCollection<MediaFile> files);

        Task DeleteObsoleteJobsAsync(string[] triggers, DateTime to);
    }
}

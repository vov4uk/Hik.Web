﻿using Autofac;
using Hik.Client.Abstraction;
using Hik.Client.Infrastructure;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Job.Impl
{
    public class ArchiveJob : JobProcessBase
    {
        private const string DateTimeFormat = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";

        public ArchiveJob(string trigger, string configFilePath, IUnitOfWorkFactory unitOfWorkFactory, Guid activityId)
            : base(trigger, unitOfWorkFactory, activityId)
        {
            Config = HikConfigExtensions.GetConfig<ArchiveConfig>(configFilePath);
            LogInfo(Config?.ToString());
        }

        protected override void CalculateProcessingPeriod()
        {
            // Not applicable
        }

        protected override async Task<IReadOnlyCollection<MediaFileDTO>> RunAsync()
        {
            IArchiveService worker = AppBootstrapper.Container.Resolve<IArchiveService>();
            worker.ExceptionFired += base.ExceptionFired;

            return await worker.ExecuteAsync(Config, DateTime.MinValue, DateTime.MaxValue);
        }

        protected override async Task SaveHistoryAsync(IReadOnlyCollection<MediaFile> files, JobService service)
        {
            await service.SaveHistoryFilesAsync<DownloadHistory>(files);
        }

        protected override async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDTO> files, JobService service)
        {
            JobInstance.PeriodStart = files.Min(x => x.Date);
            JobInstance.PeriodEnd = files.Max(x => x.Date);
            JobInstance.FilesCount = files.Count;

            var abnormalFilesCount = ((ArchiveConfig)Config).AbnormalFilesCount;
            if (abnormalFilesCount > 0 && files.Count > abnormalFilesCount)
            {
                Email.EmailHelper.Send(
                    $"{files.Count} - {TriggerKey}: Abnormal activity detected",
                    $"{files.Count} taken in period from {JobInstance.PeriodStart?.ToString(DateTimeFormat)} to {JobInstance.PeriodEnd?.ToString(DateTimeFormat)}");
            }

            await service.UpdateDailyStatisticsAsync(files);

            if (files.Sum(x => x.Duration ?? 0) > 0)
            {
                var mediaFiles = await service.SaveFilesAsync(files);
                await SaveHistoryAsync(mediaFiles, service);
            }
        }
    }
}
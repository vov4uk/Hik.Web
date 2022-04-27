using Hik.Api;
using Hik.Client.Events;
using Hik.DataAccess;
using Hik.DataAccess.Abstractions;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using Job.Extensions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Job.Impl
{
    public abstract class JobProcessBase
    {
        protected readonly Logger logger = LogManager.GetCurrentClassLogger();
        protected readonly IUnitOfWorkFactory unitOfWorkFactory;
        private JobTrigger jobTrigger;

        protected JobProcessBase(string trigger, IUnitOfWorkFactory unitOfWorkFactory, Guid activityId)
        {
            TriggerKey = trigger;
            System.Diagnostics.Trace.CorrelationManager.ActivityId = activityId;
            this.unitOfWorkFactory = unitOfWorkFactory;
        }

        public BaseConfig Config { get; protected set; }
        public string TriggerKey { get; private set; }
        protected HikJob JobInstance { get; private set; }
        public async Task ExecuteAsync()
        {
            try
            {
                await SetJobTriggerAsync();

                await CreateJobInstanceAsync();
                Config.Alias = TriggerKey;
                var result = await RunAsync();
                await SaveResultsInternalAsync(result);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        protected void ExceptionFired(object sender, ExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        protected virtual void CalculateProcessingPeriod()
        {
            var config = Config as CameraConfig;
            DateTime jobStart = DateTime.Now;

            DateTime? lastSync = jobTrigger.LastSync;
            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * config?.ProcessingPeriodHours ?? 1);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;

            LogInfo($"Last sync from DB - {lastSync}, Period - {periodStart} - {jobStart}");
        }

        protected void LogInfo(string msg)
        {
            logger.Info($"{TriggerKey} - {msg}");
        }

        protected abstract Task<IReadOnlyCollection<MediaFileDTO>> RunAsync();

        protected abstract Task SaveHistoryAsync(IReadOnlyCollection<MediaFile> files, JobService service);

        protected virtual async Task SaveResultsAsync(IReadOnlyCollection<MediaFileDTO> files, JobService service)
        {
            JobInstance.FilesCount = files.Count;
            var mediaFiles = await service.SaveFilesAsync(files);
            await service.UpdateDailyStatisticsAsync(files);
            await SaveHistoryAsync(mediaFiles, service);
        }

        private void HandleException(Exception e)
        {
            this.JobInstance.Success = false;
            try
            {
                JobService jobResultSaver = new(this.unitOfWorkFactory, this.JobInstance);
                Task.WaitAll(LogExceptionToDB(e), jobResultSaver.SaveJobResultAsync());
            }
            catch (Exception ex) { logger.Error(ex.ToString()); }

            if (Config.SentEmailOnError)
            {
                var details = JobInstance.ToHtmlTable(Config);
                EmailHelper.Send(e, Config.Alias, details);
            }
            else
            {
                logger.Error(e.ToString());
            }
        }

        private async Task CreateJobInstanceAsync()
        {
            this.JobInstance = new HikJob
            {
                Started = DateTime.Now,
                JobTriggerId = jobTrigger.Id
            };

            CalculateProcessingPeriod();

            await SaveJobInstanceToDbAsync();
        }

        private async Task SetJobTriggerAsync()
        {
            if (this.jobTrigger == null)
            {
                var jobNameParts = TriggerKey.Split(".");
                var group = jobNameParts[0];
                var triggerKey = jobNameParts[1];
                using var unitOfWork = this.unitOfWorkFactory.CreateUnitOfWork();
                var repo = unitOfWork.GetRepository<JobTrigger>();
                this.jobTrigger = await repo.FindByAsync(x => x.TriggerKey == triggerKey && x.Group == group);
                if (this.jobTrigger == null)
                {
                    jobTrigger = await repo.AddAsync(new JobTrigger { TriggerKey = triggerKey, Group = group, ShowInSearch = Config.ShowInSearch });
                    await unitOfWork.SaveChangesAsync();
                }
            }
        }

        private async Task LogExceptionToDB(Exception e)
        {
            using var unitOfWork = this.unitOfWorkFactory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<ExceptionLog>();

            await jobRepo.AddAsync(new ExceptionLog
            {
                CallStack = e.StackTrace,
                JobId = JobInstance.Id,
                Message = (e as HikException)?.ErrorMessage ?? e.ToString(),
                HikErrorCode = (e as HikException)?.ErrorCode
            });
            await unitOfWork.SaveChangesAsync();
        }

        private async Task SaveJobInstanceToDbAsync()
        {
            using var unitOfWork = this.unitOfWorkFactory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<HikJob>();
            await jobRepo.AddAsync(JobInstance);
            await unitOfWork.SaveChangesAsync();
        }

        private async Task SaveResultsInternalAsync(IReadOnlyCollection<MediaFileDTO> files)
        {
            var jobResultSaver = new JobService(this.unitOfWorkFactory, this.JobInstance);
            if (files?.Any() == true)
            {
                await SaveResultsAsync(files, jobResultSaver);
            }
            else
            {
                logger.Warn($"{TriggerKey} - Results Empty");
            }
            if (this.JobInstance.Success)
            {
                await jobResultSaver.SaveJobResultAsync();
            }
        }
    }
}
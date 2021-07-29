using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hik.Api;
using Hik.DataAccess;
using Hik.DataAccess.Data;
using Hik.DTO.Config;
using Hik.DTO.Contracts;
using Job.Email;
using NLog;
using Hik.Client.Events;
using Job.Extensions;

namespace Job.Impl
{
    public abstract class JobProcessBase
    {
        private JobTrigger jobTrigger;

        protected HikJob JobInstance { get; private set; }

        protected readonly UnitOfWorkFactory unitOfWorkFactory;

        protected readonly Logger logger = LogManager.GetCurrentClassLogger();

        public string TriggerKey { get; private set; }

        public string ConfigPath { get; private set; }

        public string ConnectionString { get; private set; }

        public BaseConfig Config { get; protected set; }

        public abstract Task<IReadOnlyCollection<MediaFileDTO>> Run();

        public abstract Task SaveHistory(IReadOnlyCollection<MediaFile> files, JobService service);

        public virtual async Task InitializeProcessingPeriodAsync()
        {
            var config = Config as CameraConfig;
            DateTime jobStart = DateTime.Now;

            var trigger = await GetJobTriggerAsync();

            DateTime? lastSync = trigger.LastSync;
            LogInfo($"Last sync from DB - {lastSync}");
            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * config?.ProcessingPeriodHours ?? 1);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;

            LogInfo($"Period - {periodStart} - {jobStart}");
        }

        public virtual async Task SaveResults(IReadOnlyCollection<MediaFileDTO> files, JobService service)
        {
            JobInstance.FilesCount = files.Count;
            var mediaFiles = await service.SaveFilesAsync(files);
            await service.UpdateDailyStatistics(files);
            await SaveHistory(mediaFiles, service);
        }

        protected JobProcessBase(string trigger, string configFilePath, string connectionString, Guid activityId)
        {
            TriggerKey = trigger;
            ConfigPath = configFilePath;
            ConnectionString = connectionString;
            System.Diagnostics.Trace.CorrelationManager.ActivityId = activityId;
            this.unitOfWorkFactory = new UnitOfWorkFactory(ConnectionString);
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await InitializeJobInstanceAsync();
                Config.Alias = TriggerKey;
                var result = await Run();
                await SaveResultsInternalAsync(result);
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        private async Task InitializeJobInstanceAsync()
        {
            var trigger = await GetJobTriggerAsync();

            this.JobInstance = new HikJob
            {
                Started = DateTime.Now,
                JobTriggerId = trigger.Id
            };

            await InitializeProcessingPeriodAsync();

            await SaveJobInstanceToDBAsync();
        }

        private async Task SaveJobInstanceToDBAsync()
        {
            using var unitOfWork = this.unitOfWorkFactory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<HikJob>();
            await jobRepo.AddAsync(JobInstance);
            await unitOfWork.SaveChangesAsync();
        }


        protected virtual void ExceptionFired(object sender, ExceptionEventArgs e)
        {
            HandleException(e.Exception);
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

        private async Task LogExceptionToDB(Exception e)
        {
            using var unitOfWork = this.unitOfWorkFactory.CreateUnitOfWork();
            var jobRepo = unitOfWork.GetRepository<ExceptionLog>();

            await jobRepo.AddAsync(new ExceptionLog
            {
                CallStack = e.StackTrace,
                JobId = JobInstance.Id,
                Message = (e as HikException)?.ErrorMessage ?? e.Message,
                HikErrorCode = (e as HikException)?.ErrorCode
            });
            await unitOfWork.SaveChangesAsync();
        }

        internal async Task SaveResultsInternalAsync(IReadOnlyCollection<MediaFileDTO> files)
        {
            LogInfo("Save to DB...");
            var jobResultSaver = new JobService(this.unitOfWorkFactory, this.JobInstance);
            if (files?.Any() == true)
            {
                await SaveResults(files, jobResultSaver);
            }
            else
            {
                logger.Warn($"{TriggerKey} - Results Empty");
            }
            if (this.JobInstance.Success)
            {
                await jobResultSaver.SaveJobResultAsync();
            }
            LogInfo("Save to DB. Done");
        }
      
        protected async Task<JobTrigger> GetJobTriggerAsync()
        {
            if (this.jobTrigger == null)
            {
                var jobNameParts = TriggerKey.Split(".");
                var triggerKey = jobNameParts[1];
                var group = jobNameParts[0];
                using var unitOfWork = this.unitOfWorkFactory.CreateUnitOfWork();
                var repo = unitOfWork.GetRepository<JobTrigger>();
                this.jobTrigger = await repo.FindByAsync(x => x.TriggerKey == triggerKey && x.Group == group);
                if (this.jobTrigger == null)
                {
                    var triggerResult = await repo.AddAsync(new JobTrigger { TriggerKey = triggerKey, Group = group });
                    jobTrigger = triggerResult.Entity;
                    await unitOfWork.SaveChangesAsync();
                }
            }
            return this.jobTrigger;
        }

        protected void LogInfo(string msg)
        {
            logger.Info($"{TriggerKey} - {msg}");
        }
    }
}

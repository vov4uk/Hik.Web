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
using Job.Extentions;

namespace Job.Impl
{
    public abstract class JobProcessBase
    {
        protected HikJob JobInstance { get; private set; }

        protected readonly UnitOfWorkFactory UnitOfWorkFactory;

        protected readonly Logger Logger = LogManager.GetCurrentClassLogger();
        public string TriggerKey { get; private set; }
        public string ConfigPath { get; private set; }
        public string ConnectionString { get; private set; }

        public string ClassName { get; private set; }
        public BaseConfig Config { get; protected set; }

        public abstract Task<IReadOnlyCollection<MediaFileDTO>> Run();

        public virtual async Task InitializeProcessingPeriod()
        {
            var config = Config as CameraConfig;
            DateTime jobStart = DateTime.Now;

            var trigger = await GetJobTrigger();

            DateTime? lastSync = trigger.LastSync;
            LogInfo($"Last sync from DB - {lastSync}");
            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * config.ProcessingPeriodHours);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;

            LogInfo($"Period - {periodStart} - {jobStart}");
        }

        public virtual async Task SaveResults(IReadOnlyCollection<MediaFileDTO> files, JobService service)
        {
            JobInstance.FilesCount = files.Count;
            await service.SaveFilesAsync(files);
        }

        protected JobProcessBase(string trigger, string configFilePath, string connectionString, Guid activityId)
        {
            TriggerKey = trigger;
            ConfigPath = configFilePath;
            ConnectionString = connectionString;
            System.Diagnostics.Trace.CorrelationManager.ActivityId = activityId;
            this.UnitOfWorkFactory = new UnitOfWorkFactory(ConnectionString);
        }

        public async Task ExecuteAsync()
        {
            try
            {
                LogInfo("InitializeJobInstance...");
                await InitializeJobInstance();
                LogInfo("InitializeJobInstance. Done.");
                Config.Alias = TriggerKey;
                LogInfo("Run...");
                var result = await Run();
                LogInfo("Run. Done.");
                LogInfo("SaveResultsInternal ...");
                await SaveResultsInternal(result);
                LogInfo("SaveResultsInternal. Done.");
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        private async Task InitializeJobInstance()
        {
            var trigger = await GetJobTrigger();

            this.JobInstance = new HikJob
            {
                Started = DateTime.Now,
                JobTriggerId = trigger.Id
            };

            await InitializeProcessingPeriod();

            await SaveJobInstanceToDB();
        }

        private async Task SaveJobInstanceToDB()
        {
            using var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork();
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
            Logger.Error(e.ToString());
            LogExceptionToDB(e);
            JobService jobResultSaver = new JobService(this.UnitOfWorkFactory, this.JobInstance);
            jobResultSaver.SaveJobResultAsync().GetAwaiter().GetResult();
            var details = JobInstance.ToHtmlTable(Config);
            EmailHelper.Send(e, Config.Alias, details);
        }

        private void LogExceptionToDB(Exception e)
        {
            try
            {
                using var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork();
                var jobRepo = unitOfWork.GetRepository<ExceptionLog>();

                jobRepo.Add(new ExceptionLog
                {
                    CallStack = e.StackTrace,
                    JobId = JobInstance.Id,
                    Message = (e as HikException)?.ErrorMessage ?? e.Message,
                    HikErrorCode = (e as HikException)?.ErrorCode
                });
                unitOfWork.SaveChanges();
            }
            catch (Exception ex) { Logger.Error(ex.ToString()); }
        }

        internal async Task SaveResultsInternal(IReadOnlyCollection<MediaFileDTO> files)
        {
            LogInfo("Save to DB...");
            var jobResultSaver = new JobService(this.UnitOfWorkFactory, this.JobInstance);
            if (files?.Any() == true)
            {
                await SaveResults(files, jobResultSaver);
            }
            else
            {
                Logger.Warn($"{TriggerKey} - Results Empty");
            }
            if (this.JobInstance.Success)
            {
                await jobResultSaver.SaveJobResultAsync();
            }
            LogInfo("Save to DB. Done");
        }
      
        protected async Task<JobTrigger> GetJobTrigger()
        {
            var jobNameParts = TriggerKey.Split(".");
            var triggerKey = jobNameParts[1];
            var group = jobNameParts[0];
            using var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork();
            var repo = unitOfWork.GetRepository<JobTrigger>();
            var trigger = await repo.FindByAsync(x => x.TriggerKey == triggerKey && x.Group == group);
            if (trigger == null)
            {
                var triggerResult = await repo.AddAsync(new JobTrigger { TriggerKey = triggerKey, Group = group});
                trigger = triggerResult.Entity;
                await unitOfWork.SaveChangesAsync();
            }
            return trigger;
        }

        protected void LogInfo(string msg)
        {
            Logger.Info($"{TriggerKey} - {msg}");
        }
    }
}

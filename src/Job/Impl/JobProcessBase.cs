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
        public BaseConfig Config { get; protected set; }
        public Parameters Parameters;
        public abstract JobType JobType { get; }

        public abstract Task<IReadOnlyCollection<MediaFileDTO>> Run();

        public virtual async Task InitializeProcessingPeriod()
        {
            var config = Config as CameraConfig;
            DateTime jobStart = DateTime.Now;

            var camera = await GetCamera(Config.Alias);
            DateTime? lastSync = camera?.LastSync;
            LogInfo($"Last sync from DB - {lastSync}");
            DateTime periodStart = lastSync?.AddMinutes(-1) ?? jobStart.AddHours(-1 * config.ProcessingPeriodHours);

            this.JobInstance.PeriodStart = periodStart;
            this.JobInstance.PeriodEnd = jobStart;

            LogInfo($"Period - {periodStart} - {jobStart}");
        }

        public virtual async Task SaveResults(IReadOnlyCollection<MediaFileDTO> files, JobService service)
        {
            JobInstance.FilesCount = files.Count;
            await service.SaveFilesAsync(files, Config);
        }

        public JobProcessBase(string trigger, string configFilePath, string connectionString, Guid activityId)
        {
            TriggerKey = trigger;
            ConfigPath = configFilePath;
            ConnectionString = connectionString;
            System.Diagnostics.Trace.CorrelationManager.ActivityId = activityId;
            this.UnitOfWorkFactory = new UnitOfWorkFactory(ConnectionString);
            LogInfo(Config.ToString());
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await InitializeJobInstance();

                var result = await Run();
                await SaveResultsInternal(result);

                LogInfo("Execution finished");
            }
            catch (Exception e)
            {
                HandleException(e);
            }
        }

        private async Task InitializeJobInstance()
        {
            this.JobInstance = new HikJob
            {
                Started = DateTime.Now,
                JobType = TriggerKey,
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
            Logger.Error(e, e.Message);
            LogExceptionToDB(e);
            var jobResultSaver = new JobService(this.UnitOfWorkFactory, this.JobInstance);
            jobResultSaver.SaveJobResultAsync(Config).GetAwaiter().GetResult();
            EmailHelper.Send(e, Config);
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
            catch (Exception ex) { Logger.Error(ex, ex.Message); }
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
                Logger.Warn($"{Config.Alias} - {JobType} - Results Empty");
            }
            if (this.JobInstance.Success)
            {
                await jobResultSaver.SaveJobResultAsync(Config);
            }
            LogInfo("Save to DB. Done");
        }

        protected async Task<Camera> GetCamera(string allias)
        {
            using var unitOfWork = this.UnitOfWorkFactory.CreateUnitOfWork();
            var cameraRepo = unitOfWork.GetRepository<Camera>();
            return await cameraRepo.FindByAsync(x => x.Alias == allias);
        }

        protected void LogInfo(string msg)
        {
            Logger.Info($"{Config.Alias} - {JobType} - {msg}");
        }
    }
}
